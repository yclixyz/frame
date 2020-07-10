using System;
using System.Collections.Generic;
using System.Text;

namespace Gugubao.Entity
{
    public class BaseEntity
    {
        public BaseEntity()
        {
            CreateTime = UpdateTime = DateTime.Now;
        }

        /// <summary>
        /// 主键
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 修改时间，自动生成
        /// </summary>
        public DateTime UpdateTime { get; set; }
    }
}
