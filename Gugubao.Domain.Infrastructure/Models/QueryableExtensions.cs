using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gugubao.Domain.Infrastructure
{
    public static class QueryableExtensions
    {
        public static async Task<List<T>> PageToListAsync<T>(this IQueryable<T> data, int curPage, int pageSize)
        {
            var skipCount = (curPage - 1) * pageSize;

            return await data.Skip(skipCount).Take(pageSize).ToListAsync();
        }

        public static PagingResult<T> ToPaging<T>(this List<T> data, long totalCount, int pageSize)
        {
            return new PagingResult<T>(pageSize, totalCount, data);
        }
    }
}
