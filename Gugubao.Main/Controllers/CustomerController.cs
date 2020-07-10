using Gugubao.Domain.Infrastructure;
using Gugubao.Extensions;
using Gugubao.Handler;
using Gugubao.Query;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Gugubao.Main.Controllers
{
    /// <summary>
    /// 客户管理
    /// </summary>
    [SwaggerTag("客户管理")]
    public class CustomerController : CustomController
    {
        private readonly CustomerQuery _customerQuery;

        public CustomerController(IMediator mediator, CustomerQuery customerQuery) : base(mediator)
        {
            _customerQuery = customerQuery;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseValue))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [SwaggerOperation(OperationId = "getbyphone", Summary = "根据手机号查询客户信息")]
        //[ResponseCache(Duration = 60, VaryByQueryKeys = new string[] { "phone" })]      
        public async Task<IActionResult> GetByPhone(string phone)
        {
            await _customerQuery.GetByPhoneAsync(phone);
            return new ResponseResult();
        }

        /// <summary>
        /// 添加客户
        /// <see cref="CreateCustomerHandler"/>
        /// </summary>
        /// <param name="command">需要添加的客户信息命令</param>
        /// <returns>返回执行结果</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseValue))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [SwaggerOperation(OperationId = "create", Summary = "添加客户")]
        public async Task<IActionResult> Create([Required, FromBody] CreateCustomerCommand command)
        {
            await _mediator.Send(command);
            return new ResponseResult();
        }

        /// <summary>
        /// 查询所有客户 
        /// </summary>
        /// <param name="model">查询过滤条件</param>
        /// <returns>符合条件的变更记录</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseValue<PagingResult<CustomerViewModel>>))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(OperationId = "getall", Summary = "根据条件查询客户信息并分页", Description = "SystemCategoryId=-1查询所有系统类别")]
        public async Task<IActionResult> GetAll([Required, FromQuery] CustomerPaingModel model)
        {
            var result = await _customerQuery.GetAllAsync(model);

            return new ResponseResult<PagingResult<CustomerViewModel>>(result);
        }
    }
}
