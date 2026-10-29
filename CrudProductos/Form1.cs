using Microsoft.EntityFrameworkCore;

namespace CrudProductos;

public partial class Form1 : Form
{
    // ---------- Controles ----------
    private readonly TextBox txtCodigo = new();
    private readonly TextBox txtNombre = new();
    private readonly TextBox txtPrecio = new();
    private readonly TextBox txtStock = new();
    private readonly TextBox txtMarca = new();

    private readonly Button btnNuevo = new();
    private readonly Button btnInsertar = new();
    private readonly Button btnActualizar = new();
    private readonly Button btnEliminar = new();
    private readonly Button btnBuscar = new();
    private readonly Button btnSalir = new();

    private readonly DataGridView dgv = new();

    private static readonly Color ColorTitulo = Color.FromArgb(30, 40, 140);
    private static readonly Color ColorBoton = Color.FromArgb(155, 185, 215);

    public Form1()
    {
        InitializeComponent();   // del Designer (vacío), se deja
        ConstruirInterfaz();
        Load += (s, e) => CargarProductos();
    }

    // =====================================================
    //  INTERFAZ
    // =====================================================
    private void ConstruirInterfaz()
    {
        Text = "Registro de Productos";
        ClientSize = new Size(640, 560);
        MinimumSize = new Size(660, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(240, 240, 240);
        Font = new Font("Segoe UI", 10);

        var lblTitulo = new Label
        {
            Text = "REGISTRO DE PRODUCTOS",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = ColorTitulo,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Left = 0,
            Top = 12,
            Width = ClientSize.Width,
            Height = 40,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        var grupo = new GroupBox
        {
            Text = "Datos de Producto",
            ForeColor = Color.FromArgb(30, 100, 200),
            Left = 20,
            Top = 65,
            Width = 380,
            Height = 270
        };

        AgregarFila(grupo, "Código", txtCodigo, 35);
        AgregarFila(grupo, "Nombre", txtNombre, 75);
        AgregarFila(grupo, "Precio", txtPrecio, 115);
        AgregarFila(grupo, "Stock", txtStock, 155);
        AgregarFila(grupo, "Marca", txtMarca, 195);

        EstilizarBoton(btnBuscar, "Buscar", 110, 228, 220, 32);
        grupo.Controls.Add(btnBuscar);

        EstilizarBoton(btnNuevo, "Nuevo", 430, 75, 180, 40);
        EstilizarBoton(btnInsertar, "Insertar", 430, 125, 180, 40);
        EstilizarBoton(btnActualizar, "Actualizar", 430, 175, 180, 40);
        EstilizarBoton(btnEliminar, "Eliminar", 430, 225, 180, 40);
        EstilizarBoton(btnSalir, "SALIR", 430, 275, 180, 40);

        ConfigurarGrid();

        Controls.AddRange(new Control[]
        {
            lblTitulo, grupo,
            btnNuevo, btnInsertar, btnActualizar, btnEliminar, btnSalir,
            dgv
        });

        // Eventos
        btnNuevo.Click += (s, e) => Nuevo();
        btnInsertar.Click += (s, e) => Insertar();
        btnActualizar.Click += (s, e) => Actualizar();
        btnEliminar.Click += (s, e) => Eliminar();
        btnBuscar.Click += (s, e) => Buscar();
        btnSalir.Click += (s, e) => Close();
        txtCodigo.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                Buscar();
                e.SuppressKeyPress = true;
            }
        };
        dgv.SelectionChanged += Dgv_SelectionChanged;
    }

    private static void AgregarFila(GroupBox grupo, string texto, TextBox txt, int y)
    {
        grupo.Controls.Add(new Label
        {
            Text = texto,
            Left = 20,
            Top = y + 4,
            AutoSize = true,
            ForeColor = Color.Black
        });
        txt.Left = 110;
        txt.Top = y;
        txt.Width = 220;
        txt.ForeColor = Color.Black;
        grupo.Controls.Add(txt);
    }

    private static void EstilizarBoton(Button b, string texto, int x, int y, int ancho, int alto)
    {
        b.Text = texto;
        b.Left = x;
        b.Top = y;
        b.Width = ancho;
        b.Height = alto;
        b.BackColor = ColorBoton;
        b.ForeColor = Color.FromArgb(30, 40, 140);
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderColor = Color.FromArgb(120, 150, 185);
        b.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        b.Cursor = Cursors.Hand;
    }

    private void ConfigurarGrid()
    {
        dgv.Left = 20;
        dgv.Top = 350;
        dgv.Width = 590;
        dgv.Height = 190;
        dgv.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
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
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(30, 40, 140);
        dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
    }

    // =====================================================
    //  CRUD
    // =====================================================

    // READ: lista todos los productos
    private void CargarProductos()
    {
        try
        {
            using var db = new ProductosDbContext();
            dgv.DataSource = db.Productos.AsNoTracking().OrderBy(p => p.Codigo).ToList();
            dgv.ClearSelection();
            LimpiarCampos();
        }
        catch (Exception ex)
        {
            MessageBox.Show("No se pudo conectar con la base de datos:\n\n" + Detalle(ex),
                "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Nuevo()
    {
        dgv.ClearSelection();
        LimpiarCampos();
        txtCodigo.Focus();
    }

    // CREATE
    private void Insertar()
    {
        if (!LeerDatos(out var producto)) return;

        try
        {
            using var db = new ProductosDbContext();

            if (db.Productos.Any(p => p.Codigo == producto.Codigo))
            {
                Aviso($"Ya existe un producto con el código {producto.Codigo}. Usa Actualizar para modificarlo.");
                return;
            }

            db.Productos.Add(producto);
            db.SaveChanges();

            CargarProductos();
            MessageBox.Show("Producto insertado correctamente.", "Éxito",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Error("Error al insertar", ex);
        }
    }

    // UPDATE
    private void Actualizar()
    {
        if (!LeerDatos(out var datos)) return;

        try
        {
            using var db = new ProductosDbContext();
            var existente = db.Productos.Find(datos.Codigo);

            if (existente == null)
            {
                Aviso($"No existe un producto con el código {datos.Codigo}. Usa Insertar para crearlo.");
                return;
            }

            existente.Nombre = datos.Nombre;
            existente.Precio = datos.Precio;
            existente.Stock = datos.Stock;
            existente.Marca = datos.Marca;
            db.SaveChanges();

            CargarProductos();
            MessageBox.Show("Producto actualizado correctamente.", "Éxito",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Error("Error al actualizar", ex);
        }
    }

    // DELETE
    private void Eliminar()
    {
        if (!int.TryParse(txtCodigo.Text.Trim(), out int codigo))
        {
            Aviso("Escribe o selecciona el código del producto a eliminar.");
            return;
        }

        var confirmar = MessageBox.Show($"¿Eliminar el producto con código {codigo}?",
            "Confirmar eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirmar != DialogResult.Yes) return;

        try
        {
            using var db = new ProductosDbContext();
            var producto = db.Productos.Find(codigo);

            if (producto == null)
            {
                Aviso($"No existe un producto con el código {codigo}.");
                return;
            }

            db.Productos.Remove(producto);
            db.SaveChanges();
            CargarProductos();
        }
        catch (Exception ex)
        {
            Error("Error al eliminar", ex);
        }
    }

    // READ: busca por código
    private void Buscar()
    {
        if (!int.TryParse(txtCodigo.Text.Trim(), out int codigo))
        {
            Aviso("Escribe un código numérico para buscar.");
            txtCodigo.Focus();
            return;
        }

        try
        {
            using var db = new ProductosDbContext();
            var p = db.Productos.AsNoTracking().FirstOrDefault(x => x.Codigo == codigo);

            if (p == null)
            {
                Aviso($"No se encontró ningún producto con el código {codigo}.");
                return;
            }

            MostrarEnCampos(p);
        }
        catch (Exception ex)
        {
            Error("Error al buscar", ex);
        }
    }

    // =====================================================
    //  AYUDAS
    // =====================================================
    private bool LeerDatos(out Producto producto)
    {
        producto = new Producto();

        if (!int.TryParse(txtCodigo.Text.Trim(), out int codigo) || codigo <= 0)
        {
            Aviso("El código debe ser un número entero positivo.");
            txtCodigo.Focus();
            return false;
        }
        if (string.IsNullOrWhiteSpace(txtNombre.Text))
        {
            Aviso("Escribe el nombre del producto.");
            txtNombre.Focus();
            return false;
        }
        if (!decimal.TryParse(txtPrecio.Text.Trim(), out decimal precio) || precio < 0)
        {
            Aviso("El precio debe ser un número válido (ej. 1250.50).");
            txtPrecio.Focus();
            return false;
        }
        if (!int.TryParse(txtStock.Text.Trim(), out int stock) || stock < 0)
        {
            Aviso("El stock debe ser un número entero (0 o mayor).");
            txtStock.Focus();
            return false;
        }
        if (string.IsNullOrWhiteSpace(txtMarca.Text))
        {
            Aviso("Escribe la marca del producto.");
            txtMarca.Focus();
            return false;
        }

        producto = new Producto
        {
            Codigo = codigo,
            Nombre = txtNombre.Text.Trim(),
            Precio = precio,
            Stock = stock,
            Marca = txtMarca.Text.Trim()
        };
        return true;
    }

    private void Dgv_SelectionChanged(object? sender, EventArgs e)
    {
        if (dgv.SelectedRows.Count == 0) return;
        if (dgv.SelectedRows[0].DataBoundItem is Producto p)
            MostrarEnCampos(p);
    }

    private void MostrarEnCampos(Producto p)
    {
        txtCodigo.Text = p.Codigo.ToString();
        txtNombre.Text = p.Nombre;
        txtPrecio.Text = p.Precio.ToString("0.00");
        txtStock.Text = p.Stock.ToString();
        txtMarca.Text = p.Marca;
    }

    private void LimpiarCampos()
    {
        txtCodigo.Clear();
        txtNombre.Clear();
        txtPrecio.Clear();
        txtStock.Clear();
        txtMarca.Clear();
    }

    private static void Aviso(string mensaje) =>
        MessageBox.Show(mensaje, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    private static void Error(string titulo, Exception ex) =>
        MessageBox.Show(Detalle(ex), titulo, MessageBoxButtons.OK, MessageBoxIcon.Error);

    private static string Detalle(Exception ex) => ex.InnerException?.Message ?? ex.Message;
}