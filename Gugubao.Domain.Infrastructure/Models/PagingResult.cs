using System;
using System.Collections.Generic;

namespace Gugubao.Domain.Infrastructure
{
    /// <summary>
    /// 分页数据
    /// </summary>
    /// <typeparam name="T">The type of Poco in the returned result set</typeparam>
    public class PagingResult<T>
    {
        public PagingResult(int pageSize, long totalCount, List<T> data)
        {
            Data = data;
            TotalPage = Math.Ceiling(totalCount / (double)pageSize);
            TotalCount = totalCount;
        }

        /// <summary>
        /// 总页数
        /// </summary>
        public double TotalPage { get; private set; }

        /// <summary>
        ///  总条数
        /// </summary>
        public long TotalCount { get; private set; }

        /// <summary>
        /// 数据集合
        /// </summary>
        public List<T> Data { get; private set; }
    }
}