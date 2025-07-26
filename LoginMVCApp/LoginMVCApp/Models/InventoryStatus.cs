namespace LoginMVCApp.Models
{
    public class InventoryStatus
    {
        public long Id { get; set; }
        public long InventoryId { get; set; }

        public bool IsOk { get; set; }
        public bool IsPolesh { get; set; }
        public bool IsNg { get; set; }
        public string? NgReason { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public long? UpdatedBy { get; set; }

        public Inventories? Inventory { get; set; }
    }
}
