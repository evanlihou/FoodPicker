using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FoodPicker.Infrastructure.Models;
using FoodPicker.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace FoodPicker.Infrastructure.Data
{
    public class MealWeekRepository : EfRepository<MealWeek>
    {
        public MealWeekRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<MealWeek> GetCurrent(CancellationToken cancellationToken = default)
        {
            return (await DbContext.MealWeeks.OrderBy(x => x.DeliveryDate).Include(x => x.Meals).ToListAsync(cancellationToken))
                .FirstOrDefault(x => x.OrderDeadline > DateTime.Now && x.OrderDeadline < DateTime.Now.AddDays(7));

        }
    }
}