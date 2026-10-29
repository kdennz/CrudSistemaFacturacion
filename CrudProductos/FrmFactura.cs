using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace CrudProductos;

// Línea que se muestra en la tabla mientras se arma la factura
public class LineaFactura
{
    public int Codigo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal Precio { get; set; }
    public decimal Subtotal => Cantidad * Precio;
}

public class FrmFactura : Form
{
    // Impuesto (ITBIS 18 %). Pon 0m si no quieres aplicar impuesto.
    private const decimal TasaImpuesto = 0.18m;

    // ---------- Controles ----------
    private readonly TextBox txtCliente = new();
    private readonly TextBox txtCodigo = new();
    private readonly Label lblInfoProducto = new();
    private readonly NumericUpDown nudCantidad = new();
    private readonly Button btnBuscar = new();
    private readonly Button btnAgregar = new();
    private readonly Button btnQuitar = new();
    private readonly Button btnNueva = new();
    private readonly Button btnGuardar = new();
    private readonly Button btnExportar = new();
    private readonly Button btnCerrar = new();
    private readonly DataGridView dgv = new();
    private readonly Label lblNumero = new();
    private readonly Label lblSubtotal = new();
    private readonly Label lblImpuesto = new();
    private readonly Label lblTotal = new();

    // ---------- Estado ----------
    private readonly BindingList<LineaFactura> _lineas = new();
    private Producto? _productoActual;
    private Factura? _facturaGuardada;

    public FrmFactura()
    {
        Text = "Facturación";
        ClientSize = new Size(900, 590);
        MinimumSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Estilo.ColorFondo;
        Font = new Font("Segoe UI", 10);
        KeyPreview = true;                      // permite capturar F5 aunque otro control tenga el foco

        ConstruirInterfaz();
        ActualizarTotales();

        KeyDown += FrmFactura_KeyDown;
        FormClosing += FrmFactura_FormClosing;
    }

    // =====================================================
    //  INTERFAZ
    // =====================================================
    private void ConstruirInterfaz()
    {
        var lblTitulo = new Label
        {
            Text = "FACTURA DE VENTA",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Estilo.ColorTitulo,
            AutoSize = true,
            Left = 20,
            Top = 12
        };

        lblNumero.Text = "Factura No.: (nueva)";
        lblNumero.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        lblNumero.ForeColor = Estilo.ColorTitulo;
        lblNumero.AutoSize = false;
        lblNumero.TextAlign = ContentAlignment.MiddleRight;
        lblNumero.SetBounds(560, 18, 320, 28);
        lblNumero.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        var lblCliente = Estilo.CrearLabel("Cliente:", 20, 66);
        txtCliente.SetBounds(90, 62, 300, 27);
        txtCliente.MaxLength = 150;

        var lblVendedor = Estilo.CrearLabel($"Atendido por: {Sesion.Actual?.NombreUsuario}", 420, 66);
        lblVendedor.ForeColor = Color.DimGray;

        // --- Agregar producto ---
        var grupo = new GroupBox
        {
            Text = "Agregar producto",
            ForeColor = Color.FromArgb(30, 100, 200),
            Left = 20,
            Top = 100,
            Width = 860,
            Height = 85,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        grupo.Controls.Add(Estilo.CrearLabel("Código:", 15, 36));
        txtCodigo.SetBounds(75, 32, 110, 27);
        Estilo.AplicarBoton(btnBuscar, "Buscar", 195, 30, 80, 30);

        lblInfoProducto.SetBounds(290, 36, 330, 24);
        lblInfoProducto.ForeColor = Color.Black;
        lblInfoProducto.AutoEllipsis = true;

        grupo.Controls.Add(Estilo.CrearLabel("Cant.:", 630, 36));
        nudCantidad.SetBounds(680, 32, 60, 27);
        nudCantidad.Minimum = 1;
        nudCantidad.Maximum = 100000;
        nudCantidad.Value = 1;
        Estilo.AplicarBoton(btnAgregar, "Agregar", 750, 30, 95, 30);

        grupo.Controls.AddRange(new Control[] { txtCodigo, btnBuscar, lblInfoProducto, nudCantidad, btnAgregar });

        // --- Tabla de líneas ---
        dgv.SetBounds(20, 200, 860, 250);
        dgv.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        Estilo.AplicarGrid(dgv);
        dgv.DataSource = _lineas;
        FormatearColumna("Codigo", "Código", null, false);
        FormatearColumna("Descripcion", "Descripción", null, false);
        FormatearColumna("Cantidad", "Cantidad", null, true);
        FormatearColumna("Precio", "Precio", "N2", true);
        FormatearColumna("Subtotal", "Subtotal", "N2", true);

        // --- Totales ---
        foreach (var l in new[] { lblSubtotal, lblImpuesto, lblTotal })
        {
            l.AutoSize = false;
            l.TextAlign = ContentAlignment.MiddleRight;
            l.Width = 220;
            l.Height = 26;
            l.Left = 660;
            l.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        }
        lblSubtotal.Top = 458;
        lblImpuesto.Top = 484;
        lblTotal.Top = 512;
        lblTotal.Height = 32;
        lblTotal.Font = new Font("Segoe UI", 14, FontStyle.Bold);
        lblTotal.ForeColor = Estilo.ColorTitulo;

        // --- Botones ---
        Estilo.AplicarBoton(btnQuitar, "Quitar línea", 20, 470, 125, 42);
        Estilo.AplicarBoton(btnNueva, "Nueva factura", 155, 470, 125, 42);
        Estilo.AplicarBoton(btnGuardar, "Guardar factura", 290, 470, 140, 42);
        Estilo.AplicarBoton(btnExportar, "Exportar (F5)", 440, 470, 125, 42);
        Estilo.AplicarBoton(btnCerrar, "Cerrar", 20, 522, 125, 42);
        foreach (var b in new[] { btnQuitar, btnNueva, btnGuardar, btnExportar, btnCerrar })
            b.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

        var lblAtajo = new Label
        {
            Text = "Atajo: pulsa F5 para imprimir la factura en un archivo PDF o TXT",
            ForeColor = Color.DimGray,
            AutoSize = true,
            Left = 155,
            Top = 535,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };

        Controls.AddRange(new Control[]
        {
            lblTitulo, lblNumero, lblCliente, txtCliente, lblVendedor, grupo, dgv,
            lblSubtotal, lblImpuesto, lblTotal,
            btnQuitar, btnNueva, btnGuardar, btnExportar, btnCerrar, lblAtajo
        });

        // --- Eventos ---
        btnBuscar.Click += (s, e) => BuscarProducto();
        txtCodigo.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                BuscarProducto();
                e.SuppressKeyPress = true;
            }
        };
        nudCantidad.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                AgregarLinea();
                e.SuppressKeyPress = true;
            }
        };
        btnAgregar.Click += (s, e) => AgregarLinea();
        btnQuitar.Click += (s, e) => QuitarLinea();
        btnNueva.Click += (s, e) => NuevaFactura();
        btnGuardar.Click += (s, e) => GuardarFactura();
        btnExportar.Click += (s, e) => Exportar();
        btnCerrar.Click += (s, e) => Close();
    }

    private void FormatearColumna(string nombre, string encabezado, string? formato, bool derecha)
    {
        if (dgv.Columns[nombre] is not DataGridViewColumn col) return;

        col.HeaderText = encabezado;
        if (formato != null)
            col.DefaultCellStyle.Format = formato;
        if (derecha)
            col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        if (nombre == "Codigo" || nombre == "Cantidad")
            col.FillWeight = 40;
        if (nombre == "Descripcion")
            col.FillWeight = 120;
    }

    // =====================================================
    //  ATAJO F5
    // =====================================================
    private void FrmFactura_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F5)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            Exportar();
        }
    }

    private void FrmFactura_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_facturaGuardada == null && _lineas.Count > 0)
        {
            var r = MessageBox.Show("Hay una factura sin guardar. ¿Deseas salir y descartarla?",
                "Factura sin guardar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r != DialogResult.Yes)
                e.Cancel = true;
        }
    }

    // =====================================================
    //  ARMAR LA FACTURA
    // =====================================================
    private void BuscarProducto()
    {
        _productoActual = null;
        lblInfoProducto.Text = "";

        if (!int.TryParse(txtCodigo.Text.Trim(), out int codigo))
        {
            Estilo.Aviso("Escribe un código numérico.");
            txtCodigo.Focus();
            return;
        }

        try
        {
            using var db = new ProductosDbContext();
            var p = db.Productos.AsNoTracking().FirstOrDefault(x => x.Codigo == codigo);

            if (p == null)
            {
                lblInfoProducto.Text = "Producto no encontrado.";
                return;
            }

            _productoActual = p;
            lblInfoProducto.Text = $"{p.Nombre}  |  Precio: {p.Precio:N2}  |  Stock: {p.Stock}";
            nudCantidad.Focus();
            nudCantidad.Select(0, nudCantidad.Text.Length);
        }
        catch (Exception ex)
        {
            Estilo.Error("Error al buscar el producto", ex);
        }
    }

    private void AgregarLinea()
    {
        if (_productoActual == null)
        {
            Estilo.Aviso("Primero busca un producto por su código (escribe el código y pulsa Enter).");
            txtCodigo.Focus();
            return;
        }

        int cantidad = (int)nudCantidad.Value;
        var existente = _lineas.FirstOrDefault(l => l.Codigo == _productoActual.Codigo);
        int yaEnFactura = existente?.Cantidad ?? 0;

        if (yaEnFactura + cantidad > _productoActual.Stock)
        {
            Estilo.Aviso($"Stock insuficiente. Disponible: {_productoActual.Stock - yaEnFactura} unidad(es).");
            return;
        }

        if (existente != null)
        {
            existente.Cantidad += cantidad;
            _lineas.ResetItem(_lineas.IndexOf(existente));
        }
        else
        {
            _lineas.Add(new LineaFactura
            {
                Codigo = _productoActual.Codigo,
                Descripcion = _productoActual.Nombre,
                Cantidad = cantidad,
                Precio = _productoActual.Precio
            });
        }

        ActualizarTotales();

        // Preparar para el siguiente producto
        _productoActual = null;
        lblInfoProducto.Text = "";
        txtCodigo.Clear();
        nudCantidad.Value = 1;
        txtCodigo.Focus();
    }

    private void QuitarLinea()
    {
        if (dgv.CurrentRow?.DataBoundItem is LineaFactura linea)
        {
            _lineas.Remove(linea);
            ActualizarTotales();
        }
        else
        {
            Estilo.Aviso("Selecciona la línea que deseas quitar.");
        }
    }

    private void ActualizarTotales()
    {
        decimal subtotal, impuesto, total;

        if (_facturaGuardada != null)
        {
            subtotal = _facturaGuardada.Subtotal;
            impuesto = _facturaGuardada.Impuesto;
            total = _facturaGuardada.Total;
        }
        else
        {
            subtotal = _lineas.Sum(l => l.Subtotal);
            impuesto = Math.Round(subtotal * TasaImpuesto, 2);
            total = subtotal + impuesto;
        }

        lblSubtotal.Text = $"Subtotal: {subtotal:N2}";
        lblImpuesto.Text = $"ITBIS ({TasaImpuesto * 100:0.##}%): {impuesto:N2}";
        lblTotal.Text = $"TOTAL: {total:N2}";
    }

    // =====================================================
    //  GUARDAR (descuenta stock y registra la venta)
    // =====================================================
    private bool GuardarFactura()
    {
        if (_facturaGuardada != null)
        {
            Estilo.Info("Esta factura ya fue guardada. Pulsa \"Nueva factura\" para crear otra.");
            return true;
        }

        if (_lineas.Count == 0)
        {
            Estilo.Aviso("Agrega al menos un producto a la factura.");
            return false;
        }

        try
        {
            using var db = new ProductosDbContext();

            var factura = new Factura
            {
                Fecha = DateTime.Now,
                Cliente = string.IsNullOrWhiteSpace(txtCliente.Text) ? "Consumidor final" : txtCliente.Text.Trim(),
                UsuarioId = Sesion.Actual!.Id,
                UsuarioNombre = Sesion.Actual.NombreUsuario
            };

            foreach (var linea in _lineas)
            {
                // Se vuelve a leer el producto de la base para validar stock y precio reales
                var producto = db.Productos.Find(linea.Codigo);

                if (producto == null)
                {
                    Estilo.Aviso($"El producto {linea.Codigo} ya no existe.");
                    return false;
                }
                if (producto.Stock < linea.Cantidad)
                {
                    Estilo.Aviso($"Stock insuficiente de \"{producto.Nombre}\". Disponible: {producto.Stock}.");
                    return false;
                }

                producto.Stock -= linea.Cantidad;   // descuenta inventario

                factura.Detalles.Add(new FacturaDetalle
                {
                    ProductoCodigo = producto.Codigo,
                    Descripcion = producto.Nombre,
                    Cantidad = linea.Cantidad,
                    PrecioUnitario = producto.Precio,
                    Subtotal = linea.Cantidad * producto.Precio
                });
            }

            factura.Subtotal = factura.Detalles.Sum(d => d.Subtotal);
            factura.Impuesto = Math.Round(factura.Subtotal * TasaImpuesto, 2);
            factura.Total = factura.Subtotal + factura.Impuesto;

            db.Facturas.Add(factura);
            db.SaveChanges();   // guarda factura, detalles y stock en una sola transacción

            _facturaGuardada = factura;
            lblNumero.Text = "Factura No.: " + factura.Id.ToString("D6");
            Bloquear(true);
            ActualizarTotales();

            Estilo.Info($"Factura {factura.Id:D6} guardada correctamente.\n\nPulsa F5 para imprimirla en PDF o TXT.");
            return true;
        }
        catch (Exception ex)
        {
            Estilo.Error("Error al guardar la factura", ex);
            return false;
        }
    }

    private void Bloquear(bool facturaGuardada)
    {
        txtCliente.ReadOnly = facturaGuardada;
        txtCodigo.ReadOnly = facturaGuardada;
        nudCantidad.Enabled = !facturaGuardada;
        btnBuscar.Enabled = !facturaGuardada;
        btnAgregar.Enabled = !facturaGuardada;
        btnQuitar.Enabled = !facturaGuardada;
        btnGuardar.Enabled = !facturaGuardada;
    }

    private void NuevaFactura()
    {
        if (_facturaGuardada == null && _lineas.Count > 0)
        {
            var r = MessageBox.Show("La factura actual no se ha guardado. ¿Descartarla y empezar una nueva?",
                "Nueva factura", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r != DialogResult.Yes) return;
        }

        _facturaGuardada = null;
        _productoActual = null;
        _lineas.Clear();
        txtCliente.Clear();
        txtCodigo.Clear();
        lblInfoProducto.Text = "";
        nudCantidad.Value = 1;
        lblNumero.Text = "Factura No.: (nueva)";
        Bloquear(false);
        ActualizarTotales();
        txtCliente.Focus();
    }

    // =====================================================
    //  IMPRIMIR / EXPORTAR  (tecla F5)
    // =====================================================
    private void Exportar()
    {
        // Si todavía no se guardó, se ofrece guardarla primero
        if (_facturaGuardada == null)
        {
            if (_lineas.Count == 0)
            {
                Estilo.Aviso("Agrega productos y guarda la factura antes de imprimirla.");
                return;
            }

            var r = MessageBox.Show("La factura aún no está guardada.\n¿Deseas guardarla ahora y exportarla?",
                "Exportar factura", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r != DialogResult.Yes) return;
            if (!GuardarFactura()) return;
        }

        var factura = _facturaGuardada!;

        using var dlg = new SaveFileDialog
        {
            Title = "Imprimir factura en archivo",
            Filter = "Documento PDF (*.pdf)|*.pdf|Archivo de texto (*.txt)|*.txt",
            FilterIndex = 1,
            FileName = "Factura_" + factura.Id.ToString("D6"),
            DefaultExt = "pdf",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            string ruta = dlg.FileName;
            string ext = Path.GetExtension(ruta).ToLowerInvariant();
            if (ext != ".pdf" && ext != ".txt")
            {
                ruta += ".pdf";
                ext = ".pdf";
            }

            var lineas = FacturaImpresion.Construir(factura);

            if (ext == ".txt")
                File.WriteAllLines(ruta, lineas, new UTF8Encoding(true));
            else
                PdfSimple.Guardar(ruta, lineas);

            var abrir = MessageBox.Show($"Factura guardada en:\n{ruta}\n\n¿Deseas abrirla ahora?",
                "Factura exportada", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (abrir == DialogResult.Yes)
                Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Estilo.Error("No se pudo exportar la factura", ex);
        }
    }
}
