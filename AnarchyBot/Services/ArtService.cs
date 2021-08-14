using AnarchyBot.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnarchyBot.Services
{
    public class ArtService
    {
        public ArtService()
        {
        }

        /// <summary>
        /// Returns amount of arts in database
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<int> CountArtsOfArtistAsync(Artist author)
        {
            if (author == null)
            {
                throw new ArgumentNullException(nameof(author));
            }

            using var context = new AnarchyBotContext();
            int count = await context.Arts.CountAsync();
            return count;
        }

        /// <summary>
        /// Returns amount of arts in database
        /// </summary>
        /// <returns></returns>
        public async Task<int> CountAsync()
        {
            using var context = new AnarchyBotContext();
            int count = await context.Arts.CountAsync();
            return count;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="link"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public async Task<Art> CreateArtAsync(string link)
        {
            link = link.Trim();
            if (Uri.TryCreate(link, UriKind.Absolute, out Uri uri) == false)
            {
                throw new FormatException(nameof(link));
            }

            var context = new AnarchyBotContext();
            Art art;

            //Check for duplicate link
            //art = await context.Arts.SingleOrDefaultAsync(x => x.Source == link);
            //if(art != null)
            //{
            //    return art;
            //}
            art = new Art() { Source = link };
            context.Arts.Add(art);
            await context.SaveChangesAsync();

            context.Entry(art);
            return art;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="author"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Art>> GetArtsAsync(int skip, int amount, Artist author = null, IEnumerable<string> tags = null)
        {
            using var context = new AnarchyBotContext();

            IQueryable<Art> query = context.Arts;

            if (author != null)
            {
                query = query.Where(x => x.AuthorId == author.Id);
            }

            int count = await query.CountAsync();
            if (count == 0)
            {
                return null;
            }

            if (tags != null)
            {
                var tagService = new TagService();

                IEnumerable<Tag> realTags = await tagService.GetTagsFromNamesAsync(tags, false);
                foreach (Tag realTag in realTags)
                {
                    query.Where(x => x.Tags.Any(x => x.TagId == realTag.Id));
                }
            }

            count = await query.CountAsync();
            if (count == 0)
            {
                return null;
            }

            IEnumerable<Art> result = await context.Arts
                .Include(art => art.Author)
                    .ThenInclude(author => author.Profiles)
                .Include(art => art.Tags)
                .Skip(skip)
                .Take(amount)
                .ToArrayAsync();

            return result;
        }

        public async Task<Art> GetRandomArtAsync(Artist author = null, IEnumerable<string> tags = null)
        {
            using var context = new AnarchyBotContext();

            IQueryable<Art> query = context.Arts;

            if (author != null)
            {
                query = query.Where(x => x.AuthorId == author.Id);
            }

            int count = await query.CountAsync();
            if (count == 0)
            {
                return null;
            }

            bool hasTags = false;
            if (tags != null)
            {
                var tagService = new TagService();

                IEnumerable<Tag> realTags = await tagService.GetTagsFromNamesAsync(tags, false);
                hasTags = realTags.Count() > 0;
                foreach (Tag realTag in realTags)
                {
                    query = query.Where(x => x.Tags.Any(x => x.TagId == realTag.Id));
                }
            }

            count = await query.CountAsync();
            if (count == 0)
            {
                return null;
            }

            if(author != null)
            {
                query = query
                    .Include(art => art.Author)
                    .ThenInclude(author => author.Profiles);
            }
            query = query
                .Include(x => x.Tags)
                .ThenInclude(x => x.Tag);

            int index = new Random().Next(0, count);
            Art result = await query
                .Skip(index)
                .FirstOrDefaultAsync();

            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="art"></param>
        /// <param name="author"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task SetAuthorAsync(Art art, Artist author)
        {
            if (art == null)
            {
                throw new ArgumentNullException(nameof(art));
            }
            else if (author == null)
            {
                throw new ArgumentNullException(nameof(author));
            }

            using var context = new AnarchyBotContext();
            var dbArt = await context.Arts.SingleOrDefaultAsync(x => x.Id == art.Id);

            art.AuthorId = author.Id;
            context.Entry(dbArt).CurrentValues.SetValues(art);
            await context.SaveChangesAsync();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="art"></param>
        /// <param name="tagsToAdd"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"
        public async Task SetTagsAsync(Art art, IEnumerable<Tag> tagsToAdd)
        {
            if (art == null)
            {
                throw new ArgumentNullException(nameof(art));
            }
            else if (tagsToAdd == null)
            {
                throw new ArgumentNullException(nameof(tagsToAdd));
            }

            using var context = new AnarchyBotContext();

            IEnumerable<ArtTag> existingTags = await context.ArtTags
                .Where(x => x.ArtId == art.Id)
                .ToListAsync();

            foreach (Tag tag in tagsToAdd)
            {
                if (existingTags.FirstOrDefault(x => x.TagId == tag.Id) == null)
                {
                    context.ArtTags.Add(new ArtTag() { ArtId = art.Id, TagId = tag.Id });
                }
            }
            await context.SaveChangesAsync();

            context.Entry(art);
        }
    }
}