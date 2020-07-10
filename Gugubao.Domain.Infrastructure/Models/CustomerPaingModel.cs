using System;
using System.Collections.Generic;
using System.Text;

namespace Gugubao.Domain.Infrastructure
{
    public class CustomerPaingModel : PagingBaseModel
    {
        public string Phone { get; set; }

        public int SystemCategoryId { get; set; }
    }
}
