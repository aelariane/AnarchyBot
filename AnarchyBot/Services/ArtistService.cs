using AnarchyBot.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnarchyBot.Services
{
    public class ArtistService
    {
        public ArtistService()
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="author"></param>
        /// <param name="link">Uri to social network profile of artist</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FormatException"></exception>
        public async Task<ArtistSocial> AddArtistSocial(Artist author, string link)
        {
            link = link.Trim();
            if (author == null)
            {
                throw new ArgumentNullException(nameof(author));
            }
            else if (Uri.TryCreate(link, UriKind.Absolute, out Uri uri) == false)
            {
                throw new FormatException(nameof(link));
            }

            using var context = new AnarchyBotContext();

            ArtistSocial existingSocial = await context.Socials
                .Where(x => x.ArtistId == author.Id)
                .FirstOrDefaultAsync(x => x.Link == link);

            if (existingSocial != null)
            {
                return existingSocial;
            }

            var social = new ArtistSocial() { Link = link, ArtistId = author.Id };
            context.Socials.Add(social);
            await context.SaveChangesAsync();

            social = context.Entry(social).Entity;
            context.Entry(author);

            return social;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="authorName"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if artist with provided name already exists</exception>
        public async Task<Artist> CreateArtistAsync(string authorName)
        {
            if (await ExistsAsync(authorName))
            {
                throw new InvalidOperationException("Artist with proided name already exists");
            }

            using var context = new AnarchyBotContext();
            Artist artist = new Artist() { NickName = authorName };

            context.Artists.Add(artist);
            await context.SaveChangesAsync();

            context.Entry(artist);
            return artist;
        }

        public async Task<bool> ExistsAsync(string authorName)
        {
            return (await GetByNicknameAsync(authorName)) != null;
        }

        public async Task<Artist> GetByNicknameAsync(string authorName)
        {
            authorName = authorName.ToLower();

            using var context = new AnarchyBotContext();
            Artist author = await context.Artists.SingleOrDefaultAsync(a => a.NickName.ToLower() == authorName);
            return author;
        }

        public async Task<IEnumerable<ArtistSocial>> GetSocials(Artist author)
        {
            if (author == null)
            {
                throw new ArgumentNullException(nameof(author));
            }

            using var context = new AnarchyBotContext();
            List<ArtistSocial> result = await context.Socials
                .Where(x => x.ArtistId == author.Id)
                .ToListAsync();

            return result;
        }
    }
}