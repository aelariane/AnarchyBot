using System.Collections.Generic;

namespace AnarchyBot.Models
{
    public class Art
    {
        public Artist Author { get; set; }
        public int? AuthorId { get; set; }
        public int Id { get; set; }
        public string Source { get; set; }
        public IList<ArtTag> Tags { get; set; }
    }
}