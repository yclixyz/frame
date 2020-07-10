using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Gugubao.Domain.Infrastructure
{
    /// <summary>
    /// 分页请求
    /// </summary>
    public class PagingBaseModel
    {
        /// <summary>
        /// 当前页
        /// </summary>
        [Range(1, int.MaxValue)]
        public int CurPage { get; set; } = 1;

        /// <summary>
        /// 每页的数量
        /// </summary>  
        [Range(1, int.MaxValue)]
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// 排序字段名称
        /// </summary>
        public string SortName { get; set; } = "CreateTime";

        /// <summary>
        /// 方向  Asc↑ Desc↓
        /// </summary>
        public string Direction { get; set; } = "Desc";
    }
}
