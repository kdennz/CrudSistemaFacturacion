using Microsoft.EntityFrameworkCore;

namespace CrudProductos;

public class FrmLogin : Form
{
    private readonly TextBox txtUsuario = new();
    private readonly TextBox txtClave = new();
    private readonly Button btnIngresar = new();
    private readonly Button btnCancelar = new();
    private int _intentos = 0;

    public FrmLogin()
    {
        Text = "Iniciar sesión";
        ClientSize = new Size(360, 300);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Estilo.ColorFondo;
        Font = new Font("Segoe UI", 10);

        var lblTitulo = new Label
        {
            Text = "INICIAR SESIÓN",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Estilo.ColorTitulo,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Left = 0,
            Top = 20,
            Width = 360,
            Height = 40
        };

        var lblSub = new Label
        {
            Text = "Ingresa tus credenciales para continuar",
            ForeColor = Color.DimGray,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Left = 0,
            Top = 62,
            Width = 360,
            Height = 24
        };

        var lblUsuario = Estilo.CrearLabel("Usuario:", 40, 105);
        txtUsuario.Left = 40;
        txtUsuario.Top = 130;
        txtUsuario.Width = 280;
        txtUsuario.MaxLength = 50;

        var lblClave = Estilo.CrearLabel("Contraseña:", 40, 170);
        txtClave.Left = 40;
        txtClave.Top = 195;
        txtClave.Width = 280;
        txtClave.UseSystemPasswordChar = true;
        txtClave.MaxLength = 100;

        Estilo.AplicarBoton(btnIngresar, "Ingresar", 40, 240, 135, 40);
        Estilo.AplicarBoton(btnCancelar, "Cancelar", 185, 240, 135, 40);
        btnCancelar.DialogResult = DialogResult.Cancel;

        Controls.AddRange(new Control[]
        {
            lblTitulo, lblSub, lblUsuario, txtUsuario, lblClave, txtClave, btnIngresar, btnCancelar
        });

        AcceptButton = btnIngresar;   // Enter = Ingresar
        CancelButton = btnCancelar;   // Esc = Cancelar

        btnIngresar.Click += (s, e) => Ingresar();
    }

    private void Ingresar()
    {
        var usuario = txtUsuario.Text.Trim();
        var clave = txtClave.Text;

        if (usuario == "" || clave == "")
        {
            Estilo.Aviso("Escribe tu usuario y contraseña.");
            return;
        }

        try
        {
            using var db = new ProductosDbContext();
            var u = db.Usuarios.AsNoTracking()
                .FirstOrDefault(x => x.NombreUsuario == usuario && x.Activo);

            if (u == null || !Seguridad.Verificar(clave, u.PasswordHash))
            {
                _intentos++;

                if (_intentos >= 3)
                {
                    MessageBox.Show("Demasiados intentos fallidos. La aplicación se cerrará.",
                        "Acceso denegado", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    DialogResult = DialogResult.Cancel;
                    return;
                }

                // Mensaje genérico: no revela si el usuario existe o no
                Estilo.Aviso($"Usuario o contraseña incorrectos.\nIntentos restantes: {3 - _intentos}");
                txtClave.Clear();
                txtClave.Focus();
                return;
            }

            Sesion.Actual = u;
            DialogResult = DialogResult.OK;   // cierra el login
        }
        catch (Exception ex)
        {
            Estilo.Error("Error de conexión", ex);
        }
    }
}
