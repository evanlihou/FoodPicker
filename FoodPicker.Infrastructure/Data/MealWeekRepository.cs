using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FoodPicker.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodPicker.Infrastructure.Data
{
    public class MealWeekRepository : EfRepository<MealWeek>
    {
        public MealWeekRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<MealWeek> GetByIdWithMealsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await DbContext.MealWeeks.Include(x => x.Meals)
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        /// <summary>
        /// Get the week which is upcoming, where voting the order deadline is in the future but not more than 7 days away
        /// </summary>
        /// <param name="cancellationToken">An optional cancellation token to be passed to the DB calls</param>
        /// <returns>The week, or null if a week matching the criteria is not found</returns>
        public async Task<MealWeek> GetCurrentWithMeals(CancellationToken cancellationToken = default)
        {
            return (await DbContext.MealWeeks.OrderBy(x => x.DeliveryDate).Include(x => x.Meals)
                .ToListAsync(cancellationToken)).FirstOrDefault(x =>
                x.OrderDeadline > DateTime.Now && x.OrderDeadline < DateTime.Now.AddDays(7));
        }

        /// <summary>
        /// Get the latest `MealWeek`, regardless of status or order deadline
        /// </summary>
        /// <param name="cancellationToken">An optional cancellation token to be passed to the DB calls</param>
        /// <returns>The week, or null if no weeks are in the database</returns>
        public async Task<MealWeek> GetLatest(CancellationToken cancellationToken = default)
        {
            return await DbContext.MealWeeks.OrderByDescending(x => x.DeliveryDate)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}