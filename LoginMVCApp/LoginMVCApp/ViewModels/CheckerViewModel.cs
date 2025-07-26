using LoginMVCApp.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LoginMVCApp.ViewModels
{
    public class CheckerViewModel
    {
        public Inventories? Inventory { get; set; }
        public long? SelectedRobotId { get; set; }
        public List<SelectListItem> RobotList { get; set; } = new();
    }

}
