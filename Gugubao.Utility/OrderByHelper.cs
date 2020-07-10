using System;
using System.Collections.Generic;
using System.Text;

namespace Gugubao.Utility
{
    public class OrderByHelper
    {
        /// <summary>
        /// 合并排序字符串，默认按照创建时间倒序
        /// </summary>
        /// <param name="sortName">排序字段</param>
        /// <param name="direction">方向 Asc Desc</param>
        /// <returns>返回合并后的字符串" Name Direction "</returns>
        public static string Commbin(string sortName, string direction)
        {
            if (string.IsNullOrWhiteSpace(sortName) ||
                string.IsNullOrEmpty(direction))
            {
                return $"createtime desc";
            }

            return $"{sortName} {direction} ";
        }
    }
}
