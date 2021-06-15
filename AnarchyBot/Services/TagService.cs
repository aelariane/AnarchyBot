using AnarchyBot.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnarchyBot.Services
{
    public class TagService
    {
        public TagService()
        {
        }

        public async Task<int> CountAsync()
        {
            using var context = new AnarchyBotContext();
            return await context.Tags.CountAsync();
        }

        public async Task<IList<Tag>> GeTagListAsync()
        {
            using var context = new AnarchyBotContext();
            return await context.Tags.ToListAsync();
        }

        public async Task<Tag> GetByName(string tagName, bool createIfNotExists)
        {
            using var context = new AnarchyBotContext();

            tagName = tagName.Trim().ToLower();
            Tag tag = await context.Tags.SingleOrDefaultAsync(x => x.Name == tagName);

            if (tag == null && createIfNotExists)
            {
                tag = new Tag() { Name = tagName };
                context.Tags.Add(tag);
                await context.SaveChangesAsync();

                context.Entry(tag);
            }

            return tag;
        }

        public async Task<IEnumerable<Tag>> GetTagsFromNamesAsync(IEnumerable<string> tagNames, bool createIfNotExist)
        {
            using var context = new AnarchyBotContext();

            string[] tagNamesLower = tagNames
                .Select(x => x.Trim().ToLower())
                .ToArray();

            int initialCount = tagNamesLower.Length;

            int counter = 0;
            string inString = "(" + string.Join(", ", tagNamesLower.Select(x => "@" + (counter++).ToString())) + ")";
            SqlParameter[] parameters = Enumerable.Range(0, counter)
                .Select(i => new SqlParameter("@" + i.ToString(), tagNamesLower[i]))
                .ToArray();
            string selectQuery = $"SELECT * FROM Tags WHERE Name IN {inString}";

            IList<Tag> resultTags = await context.Tags
                .FromSqlRaw(selectQuery, parameters)
                .ToListAsync();

            if (resultTags.Count != initialCount && createIfNotExist)
            {
                foreach (string tagName in tagNamesLower)
                {
                    if (resultTags.FirstOrDefault(x => x.Name == tagName) == null)
                    {
                        Tag newTag = new Tag() { Name = tagName };
                        context.Tags.Add(newTag);
                        await context.SaveChangesAsync();

                        context.Entry(newTag);
                        resultTags.Add(newTag);
                    }
                }
            }

            return resultTags;
        }
    }
}