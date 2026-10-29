using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrudProductos;

[Table("Productos")]
public class Producto
{
    // El código lo escribe el usuario (no es autoincremental)
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Codigo { get; set; }

    [Required, MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Precio { get; set; }

    public int Stock { get; set; }

    [Required, MaxLength(100)]
    public string Marca { get; set; } = string.Empty;
}