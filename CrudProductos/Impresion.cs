using System.Globalization;
using System.Text;

namespace CrudProductos;

// ---------------------------------------------------------------
//  Arma las líneas de texto de la factura (sirve para .txt y .pdf)
// ---------------------------------------------------------------
public static class FacturaImpresion
{
    // Cambia estos datos por los de tu negocio
    public const string NombreEmpresa = "MI TIENDA, S.R.L.";
    public const string Direccion = "Dirección de la empresa  |  Tel. 809-000-0000";

    private const int Ancho = 64;

    public static List<string> Construir(Factura f)
    {
        var l = new List<string>
        {
            Centrar(NombreEmpresa),                 // la primera línea es el título (negrita en el PDF)
            Centrar(Direccion),
            Centrar("FACTURA DE VENTA"),
            new string('=', Ancho),
            "Factura No.: " + f.Id.ToString("D6"),
            "Fecha      : " + f.Fecha.ToString("dd/MM/yyyy HH:mm"),
            "Cliente    : " + f.Cliente,
            "Atendido   : " + f.UsuarioNombre,
            new string('-', Ancho),
            Fila("Cod", "Descripción", "Cant", "Precio", "Subtotal"),
            new string('-', Ancho)
        };

        foreach (var d in f.Detalles)
        {
            l.Add(Fila(
                d.ProductoCodigo.ToString(),
                Cortar(d.Descripcion, 24),
                d.Cantidad.ToString(),
                d.PrecioUnitario.ToString("N2"),
                d.Subtotal.ToString("N2")));
        }

        l.Add(new string('-', Ancho));
        l.Add(Total("Subtotal:", f.Subtotal));
        l.Add(Total("ITBIS:", f.Impuesto));
        l.Add(Total("TOTAL:", f.Total));
        l.Add(new string('=', Ancho));
        l.Add(Centrar("¡Gracias por su compra!"));

        return l;
    }

    // Columnas: Cod(6) Descripción(24) Cant(5) Precio(12) Subtotal(13) = 64 caracteres
    private static string Fila(string cod, string desc, string cant, string precio, string subtotal) =>
        cod.PadRight(6) + " " + desc.PadRight(24) + " " + cant.PadLeft(5) + " " +
        precio.PadLeft(12) + " " + subtotal.PadLeft(13);

    private static string Total(string etiqueta, decimal valor) =>
        etiqueta.PadLeft(51) + " " + valor.ToString("N2").PadLeft(12);

    private static string Centrar(string texto)
    {
        if (texto.Length >= Ancho) return texto;
        return new string(' ', (Ancho - texto.Length) / 2) + texto;
    }

    private static string Cortar(string texto, int max) =>
        texto.Length <= max ? texto : texto.Substring(0, max);
}

// ---------------------------------------------------------------
//  Escritor de PDF mínimo (texto monoespaciado, sin librerías)
//  - Fuente Courier estándar de PDF (no necesita instalarse)
//  - Página A4, con salto automático de página
// ---------------------------------------------------------------
public static class PdfSimple
{
    private const int LineasPorPagina = 52;

    public static void Guardar(string ruta, IReadOnlyList<string> lineas)
    {
        string titulo = lineas.Count > 0 ? lineas[0].Trim() : "";
        var cuerpo = lineas.Skip(1).ToList();

        // Separar el cuerpo en páginas
        var paginas = new List<List<string>>();
        for (int i = 0; i < cuerpo.Count; i += LineasPorPagina)
            paginas.Add(cuerpo.Skip(i).Take(LineasPorPagina).ToList());
        if (paginas.Count == 0)
            paginas.Add(new List<string>());

        var latin1 = Encoding.Latin1;
        using var ms = new MemoryStream();
        var posiciones = new List<long>();   // posición de cada objeto (para la tabla xref)

        void Escribir(string s)
        {
            byte[] b = latin1.GetBytes(s);
            ms.Write(b, 0, b.Length);
        }

        void IniciarObjeto(int numero)
        {
            posiciones.Add(ms.Position);
            Escribir(numero.ToString(CultureInfo.InvariantCulture) + " 0 obj\n");
        }

        Escribir("%PDF-1.4\n");

        // 1: catálogo
        IniciarObjeto(1);
        Escribir("<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

        // 2: lista de páginas
        var kids = string.Join(" ", Enumerable.Range(0, paginas.Count)
            .Select(i => (5 + 2 * i).ToString(CultureInfo.InvariantCulture) + " 0 R"));
        IniciarObjeto(2);
        Escribir("<< /Type /Pages /Kids [" + kids + "] /Count " + paginas.Count.ToString(CultureInfo.InvariantCulture) + " >>\nendobj\n");

        // 3 y 4: fuentes
        IniciarObjeto(3);
        Escribir("<< /Type /Font /Subtype /Type1 /BaseFont /Courier /Encoding /WinAnsiEncoding >>\nendobj\n");
        IniciarObjeto(4);
        Escribir("<< /Type /Font /Subtype /Type1 /BaseFont /Courier-Bold /Encoding /WinAnsiEncoding >>\nendobj\n");

        // 5..: cada página (objeto página + objeto contenido)
        for (int i = 0; i < paginas.Count; i++)
        {
            int objPagina = 5 + 2 * i;
            int objContenido = objPagina + 1;

            IniciarObjeto(objPagina);
            Escribir("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] " +
                     "/Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> " +
                     "/Contents " + objContenido.ToString(CultureInfo.InvariantCulture) + " 0 R >>\nendobj\n");

            byte[] contenido = latin1.GetBytes(ConstruirContenido(titulo, paginas[i], i == 0));

            IniciarObjeto(objContenido);
            Escribir("<< /Length " + contenido.Length.ToString(CultureInfo.InvariantCulture) + " >>\nstream\n");
            ms.Write(contenido, 0, contenido.Length);
            Escribir("\nendstream\nendobj\n");
        }

        // Tabla de referencias cruzadas
        long inicioXref = ms.Position;
        int total = posiciones.Count + 1;
        Escribir("xref\n0 " + total.ToString(CultureInfo.InvariantCulture) + "\n");
        Escribir("0000000000 65535 f \n");
        foreach (long pos in posiciones)
            Escribir(pos.ToString("D10", CultureInfo.InvariantCulture) + " 00000 n \n");

        Escribir("trailer\n<< /Size " + total.ToString(CultureInfo.InvariantCulture) + " /Root 1 0 R >>\n" +
                 "startxref\n" + inicioXref.ToString(CultureInfo.InvariantCulture) + "\n%%EOF\n");

        File.WriteAllBytes(ruta, ms.ToArray());
    }

    private static string ConstruirContenido(string titulo, List<string> lineas, bool primeraPagina)
    {
        var sb = new StringBuilder();
        int yInicio = 800;

        if (primeraPagina && titulo.Length > 0)
        {
            // Título centrado, Courier-Bold 14 pt (cada carácter mide ~8.4 pt)
            double x = Math.Max(50, (595 - titulo.Length * 8.4) / 2);
            sb.Append("BT\n/F2 14 Tf\n");
            sb.Append(x.ToString("0.0", CultureInfo.InvariantCulture)).Append(" 800 Td\n");
            sb.Append('(').Append(Escapar(titulo)).Append(") Tj\nET\n");
            yInicio = 770;
        }

        sb.Append("BT\n/F1 10 Tf\n12 TL\n50 ");
        sb.Append(yInicio.ToString(CultureInfo.InvariantCulture)).Append(" Td\n");

        for (int i = 0; i < lineas.Count; i++)
        {
            if (i > 0) sb.Append("T*\n");
            sb.Append('(').Append(Escapar(lineas[i])).Append(") Tj\n");
        }
        sb.Append("ET\n");

        return sb.ToString();
    }

    // En PDF hay que escapar \ ( )
    private static string Escapar(string s) =>
        s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}
