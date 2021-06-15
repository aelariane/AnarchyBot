using System.Collections.Generic;

namespace AnarchyBot.Models
{
    public class Artist
    {
        public IList<Art> Arts { get; set; }
        public int Id { get; set; }
        public string NickName { get; set; }
        public IList<ArtistSocial> Profiles { get; set; }
    }
}