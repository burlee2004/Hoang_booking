using Bookify.Data.Data;
using Bookify.Data.Models;
using Bookify.Services.Generic;

namespace Bookify.Services.ModelsRepos
{
    public class ContactMessageRepo : GenericRepository<ContactMessage>
    {
        public ContactMessageRepo(AppDbContext dbContext) : base(dbContext)
        {
        }
    }
}