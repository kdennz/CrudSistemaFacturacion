using System.Security.Cryptography;

namespace CrudProductos;

// ---------------------------------------------------------------
//  Cifrado de contraseñas (PBKDF2 + sal aleatoria)
//  Nunca se guarda la contraseña real en la base de datos.
// ---------------------------------------------------------------
public static class Seguridad
{
    private const int Iteraciones = 100_000;

    public static string HashPassword(string password)
    {
        byte[] sal = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, sal, Iteraciones, HashAlgorithmName.SHA256, 32);
        return $"{Iteraciones}.{Convert.ToBase64String(sal)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verificar(string password, string almacenado)
    {
        try
        {
            var partes = almacenado.Split('.');
            if (partes.Length != 3 || !int.TryParse(partes[0], out int iteraciones))
                return false;

            byte[] sal = Convert.FromBase64String(partes[1]);
            byte[] esperado = Convert.FromBase64String(partes[2]);
            byte[] calculado = Rfc2898DeriveBytes.Pbkdf2(password, sal, iteraciones, HashAlgorithmName.SHA256, esperado.Length);
            return CryptographicOperations.FixedTimeEquals(calculado, esperado);
        }
        catch
        {
            return false;
        }
    }
}

// ---------------------------------------------------------------
//  Usuario que inició sesión
// ---------------------------------------------------------------
public static class Sesion
{
    public static Usuario? Actual { get; set; }
    public static bool EsAdmin => Actual?.Rol == "Administrador";
}

// ---------------------------------------------------------------
//  Estilos y mensajes compartidos por todos los formularios
// ---------------------------------------------------------------
public static class Estilo
{
    public static readonly Color ColorTitulo = Color.FromArgb(30, 40, 140);
    public static readonly Color ColorBoton = Color.FromArgb(155, 185, 215);
    public static readonly Color ColorFondo = Color.FromArgb(240, 240, 240);

    public static void AplicarBoton(Button b, string texto, int x, int y, int ancho, int alto)
    {
        b.Text = texto;
        b.Left = x;
        b.Top = y;
        b.Width = ancho;
        b.Height = alto;
        b.BackColor = ColorBoton;
        b.ForeColor = ColorTitulo;
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderColor = Color.FromArgb(120, 150, 185);
        b.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        b.Cursor = Cursors.Hand;
    }

    public static Label CrearLabel(string texto, int x, int y, bool negrita = false) => new()
    {
        Text = texto,
        Left = x,
        Top = y,
        AutoSize = true,
        ForeColor = Color.Black,
        Font = new Font("Segoe UI", 10, negrita ? FontStyle.Bold : FontStyle.Regular)
    };

    public static void AplicarGrid(DataGridView dgv)
    {
        dgv.ReadOnly = true;
        dgv.AllowUserToAddRows = false;
        dgv.AllowUserToDeleteRows = false;
        dgv.MultiSelect = false;
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgv.RowHeadersVisible = false;
        dgv.BackgroundColor = Color.White;
        dgv.EnableHeadersVisualStyles = false;
        dgv.ColumnHeadersDefaultCellStyle.BackColor = ColorBoton;
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = ColorTitulo;
        dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
    }

    public static void Aviso(string mensaje) =>
        MessageBox.Show(mensaje, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    public static void Info(string mensaje) =>
        MessageBox.Show(mensaje, "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);

    public static void Error(string titulo, Exception ex) =>
        MessageBox.Show(Detalle(ex), titulo, MessageBoxButtons.OK, MessageBoxIcon.Error);

    public static string Detalle(Exception ex) => ex.InnerException?.Message ?? ex.Message;
}
