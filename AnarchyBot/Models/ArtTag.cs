namespace AnarchyBot.Models
{
    public class ArtTag
    {
        public Art Art { get; set; }
        public int ArtId { get; set; }
        public Tag Tag { get; set; }
        public int TagId { get; set; }
    }
}