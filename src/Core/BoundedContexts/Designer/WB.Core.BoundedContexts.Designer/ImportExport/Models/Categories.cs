using System;

namespace WB.Core.BoundedContexts.Designer.ImportExport.Models
{
    public class Categories
    {
        public Guid? Id { get; set; }
        public string Name { get; set; } = String.Empty;
        public string FileName { get; set; } = String.Empty;
    }
}
