using MediatR;

namespace Gugubao.Domain.Infrastructure
{
    public class CreateCustomerCommand : IRequest<bool>
    {
        /// <summary>
        /// 手机号
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// 客户所属系统类别
        /// </summary>
        public int SystemCategoryId { get; set; }
    }
}
