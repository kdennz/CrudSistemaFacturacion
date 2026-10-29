using Microsoft.EntityFrameworkCore;

namespace CrudProductos;

// Pantalla solo para administradores: crear usuarios, cambiar contraseñas, activar/desactivar
public class FrmUsuarios : Form
{
    private readonly TextBox txtUsuario = new();
    private readonly TextBox txtClave = new();
    private readonly ComboBox cmbRol = new();
    private readonly Button btnCrear = new();
    private readonly Button btnClave = new();
    private readonly Button btnEstado = new();
    private readonly Button btnCerrar = new();
    private readonly DataGridView dgv = new();

    public FrmUsuarios()
    {
        // Seguridad extra: aunque el botón no se muestre a otros roles, se valida aquí también
        if (!Sesion.EsAdmin)
        {
            Estilo.Aviso("Solo un administrador puede gestionar usuarios.");
            Load += (s, e) => Close();
            return;
        }

        Text = "Administración de usuarios";
        ClientSize = new Size(640, 480);
        MinimumSize = new Size(640, 500);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Estilo.ColorFondo;
        Font = new Font("Segoe UI", 10);

        var grupo = new GroupBox
        {
            Text = "Datos del usuario",
            ForeColor = Color.FromArgb(30, 100, 200),
            Left = 20,
            Top = 15,
            Width = 600,
            Height = 160,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        grupo.Controls.Add(Estilo.CrearLabel("Usuario", 15, 38));
        txtUsuario.SetBounds(110, 34, 200, 27);
        txtUsuario.MaxLength = 50;

        grupo.Controls.Add(Estilo.CrearLabel("Contraseña", 15, 78));
        txtClave.SetBounds(110, 74, 200, 27);
        txtClave.UseSystemPasswordChar = true;
        txtClave.MaxLength = 100;

        grupo.Controls.Add(Estilo.CrearLabel("Rol", 15, 118));
        cmbRol.SetBounds(110, 114, 200, 27);
        cmbRol.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbRol.Items.AddRange(new object[] { "Usuario", "Administrador" });
        cmbRol.SelectedIndex = 0;

        Estilo.AplicarBoton(btnCrear, "Crear usuario", 340, 32, 240, 32);
        Estilo.AplicarBoton(btnClave, "Cambiar contraseña", 340, 72, 240, 32);
        Estilo.AplicarBoton(btnEstado, "Activar / Desactivar", 340, 112, 240, 32);

        grupo.Controls.AddRange(new Control[] { txtUsuario, txtClave, cmbRol, btnCrear, btnClave, btnEstado });

        dgv.SetBounds(20, 190, 600, 235);
        dgv.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        Estilo.AplicarGrid(dgv);

        Estilo.AplicarBoton(btnCerrar, "Cerrar", 500, 435, 120, 36);
        btnCerrar.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

        Controls.AddRange(new Control[] { grupo, dgv, btnCerrar });

        btnCrear.Click += (s, e) => CrearUsuario();
        btnClave.Click += (s, e) => CambiarClave();
        btnEstado.Click += (s, e) => CambiarEstado();
        btnCerrar.Click += (s, e) => Close();

        Load += (s, e) => CargarUsuarios();
    }

    private void CargarUsuarios()
    {
        try
        {
            using var db = new ProductosDbContext();
            dgv.DataSource = db.Usuarios.AsNoTracking()
                .OrderBy(u => u.Id)
                .Select(u => new { u.Id, Usuario = u.NombreUsuario, u.Rol, u.Activo })
                .ToList();
        }
        catch (Exception ex)
        {
            Estilo.Error("Error al cargar usuarios", ex);
        }
    }

    private int? IdSeleccionado()
    {
        if (dgv.CurrentRow?.Cells["Id"].Value is int id)
            return id;
        return null;
    }

    private void CrearUsuario()
    {
        var nombre = txtUsuario.Text.Trim();
        var clave = txtClave.Text;

        if (nombre.Length < 3)
        {
            Estilo.Aviso("El nombre de usuario debe tener al menos 3 caracteres.");
            txtUsuario.Focus();
            return;
        }
        if (clave.Length < 6)
        {
            Estilo.Aviso("La contraseña debe tener al menos 6 caracteres.");
            txtClave.Focus();
            return;
        }

        try
        {
            using var db = new ProductosDbContext();

            if (db.Usuarios.Any(u => u.NombreUsuario == nombre))
            {
                Estilo.Aviso("Ya existe un usuario con ese nombre.");
                return;
            }

            db.Usuarios.Add(new Usuario
            {
                NombreUsuario = nombre,
                PasswordHash = Seguridad.HashPassword(clave),
                Rol = cmbRol.SelectedItem?.ToString() ?? "Usuario",
                Activo = true
            });
            db.SaveChanges();

            txtUsuario.Clear();
            txtClave.Clear();
            cmbRol.SelectedIndex = 0;
            CargarUsuarios();
            Estilo.Info("Usuario creado correctamente.");
        }
        catch (Exception ex)
        {
            Estilo.Error("Error al crear usuario", ex);
        }
    }

    private void CambiarClave()
    {
        var id = IdSeleccionado();
        if (id == null)
        {
            Estilo.Aviso("Selecciona un usuario de la tabla.");
            return;
        }
        if (txtClave.Text.Length < 6)
        {
            Estilo.Aviso("Escribe la nueva contraseña (mínimo 6 caracteres) en el campo Contraseña.");
            txtClave.Focus();
            return;
        }

        try
        {
            using var db = new ProductosDbContext();
            var u = db.Usuarios.Find(id.Value);
            if (u == null) return;

            u.PasswordHash = Seguridad.HashPassword(txtClave.Text);
            db.SaveChanges();

            txtClave.Clear();
            Estilo.Info($"Contraseña de '{u.NombreUsuario}' actualizada.");
        }
        catch (Exception ex)
        {
            Estilo.Error("Error al cambiar la contraseña", ex);
        }
    }

    private void CambiarEstado()
    {
        var id = IdSeleccionado();
        if (id == null)
        {
            Estilo.Aviso("Selecciona un usuario de la tabla.");
            return;
        }
        if (id == Sesion.Actual!.Id)
        {
            Estilo.Aviso("No puedes desactivar tu propio usuario.");
            return;
        }

        try
        {
            using var db = new ProductosDbContext();
            var u = db.Usuarios.Find(id.Value);
            if (u == null) return;

            u.Activo = !u.Activo;
            db.SaveChanges();
            CargarUsuarios();
        }
        catch (Exception ex)
        {
            Estilo.Error("Error al cambiar el estado", ex);
        }
    }
}
