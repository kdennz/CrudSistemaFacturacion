using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrudProductos;

[Table("Usuarios")]
public class Usuario
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string PasswordHash { get; set; } = string.Empty;

    // "Administrador" o "Usuario"
    [Required, MaxLength(20)]
    public string Rol { get; set; } = "Usuario";

    public bool Activo { get; set; } = true;
}

[Table("Facturas")]
public class Factura
{
    [Key]
    public int Id { get; set; }                 // número de factura

    public DateTime Fecha { get; set; }

    [Required, MaxLength(150)]
    public string Cliente { get; set; } = string.Empty;

    public int UsuarioId { get; set; }

    [Required, MaxLength(50)]
    public string UsuarioNombre { get; set; } = string.Empty;

    [Column(TypeName = "decimal(12,2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal Impuesto { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal Total { get; set; }

    public List<FacturaDetalle> Detalles { get; set; } = new();
}

[Table("FacturaDetalles")]
public class FacturaDetalle
{
    [Key]
    public int Id { get; set; }

    public int FacturaId { get; set; }
    public Factura? Factura { get; set; }

    public int ProductoCodigo { get; set; }

    [Required, MaxLength(100)]
    public string Descripcion { get; set; } = string.Empty;

    public int Cantidad { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal PrecioUnitario { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal Subtotal { get; set; }
}
