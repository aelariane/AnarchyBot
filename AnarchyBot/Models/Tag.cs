using System.Collections.Generic;

namespace AnarchyBot.Models
{
    public class Tag
    {
        public IList<ArtTag> Arts { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
    }
}