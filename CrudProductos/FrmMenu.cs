namespace CrudProductos;

public class FrmMenu : Form
{
    // true = el usuario pulsó "Cerrar sesión" (vuelve al login)
    public bool CerrarSesion { get; private set; }

    public FrmMenu()
    {
        var usuario = Sesion.Actual!;

        Text = "Sistema de Ventas - Menú principal";
        ClientSize = new Size(420, 420);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Estilo.ColorFondo;
        Font = new Font("Segoe UI", 10);

        var lblTitulo = new Label
        {
            Text = "MENÚ PRINCIPAL",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Estilo.ColorTitulo,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Left = 0,
            Top = 15,
            Width = 420,
            Height = 45
        };

        var lblUsuario = new Label
        {
            Text = $"Usuario: {usuario.NombreUsuario}   |   Rol: {usuario.Rol}",
            ForeColor = Color.DimGray,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Left = 0,
            Top = 62,
            Width = 420,
            Height = 24
        };

        Controls.Add(lblTitulo);
        Controls.Add(lblUsuario);

        int y = 105;

        // Todos los usuarios pueden facturar
        var btnFactura = CrearBoton("Facturación (Ventas)", ref y);
        btnFactura.Click += (s, e) =>
        {
            using var f = new FrmFactura();
            f.ShowDialog(this);
        };

        // Solo el administrador gestiona productos y usuarios
        if (Sesion.EsAdmin)
        {
            var btnProductos = CrearBoton("Productos (CRUD)", ref y);
            btnProductos.Click += (s, e) =>
            {
                using var f = new Form1();
                f.ShowDialog(this);
            };

            var btnUsuarios = CrearBoton("Usuarios", ref y);
            btnUsuarios.Click += (s, e) =>
            {
                using var f = new FrmUsuarios();
                f.ShowDialog(this);
            };
        }

        y += 10;
        var btnCerrarSesion = CrearBoton("Cerrar sesión", ref y);
        btnCerrarSesion.Click += (s, e) =>
        {
            Sesion.Actual = null;
            CerrarSesion = true;
            Close();
        };

        var btnSalir = CrearBoton("Salir", ref y);
        btnSalir.Click += (s, e) => Close();
    }

    private Button CrearBoton(string texto, ref int y)
    {
        var b = new Button();
        Estilo.AplicarBoton(b, texto, 80, y, 260, 44);
        Controls.Add(b);
        y += 56;
        return b;
    }
}
