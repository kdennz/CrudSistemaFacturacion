namespace CrudProductos;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Crea el usuario administrador inicial si todavía no existe ninguno
        if (!PrepararUsuarioAdmin()) return;

        while (true)
        {
            // 1) Login obligatorio
            using (var login = new FrmLogin())
            {
                if (login.ShowDialog() != DialogResult.OK)
                    return;                         // canceló o falló: se cierra la app
            }

            // 2) Menú principal
            using var menu = new FrmMenu();
            Application.Run(menu);

            // Si cerró sesión vuelve al login; si cerró la ventana, termina
            if (!menu.CerrarSesion)
                return;
        }
    }

    private static bool PrepararUsuarioAdmin()
    {
        try
        {
            using var db = new ProductosDbContext();

            if (!db.Usuarios.Any())
            {
                db.Usuarios.Add(new Usuario
                {
                    NombreUsuario = "admin",
                    PasswordHash = Seguridad.HashPassword("Admin123"),
                    Rol = "Administrador",
                    Activo = true
                });
                db.SaveChanges();

                MessageBox.Show(
                    "Se creó el usuario administrador inicial:\n\n" +
                    "   Usuario: admin\n   Contraseña: Admin123\n\n" +
                    "Por seguridad, cambia la contraseña desde el menú Usuarios.",
                    "Primer inicio", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "No se pudo acceder a la base de datos.\n\n" +
                "¿Ejecutaste el script 3_script_facturacion.sql en SSMS?\n\n" +
                "Detalle: " + Estilo.Detalle(ex),
                "Error de base de datos", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }
}