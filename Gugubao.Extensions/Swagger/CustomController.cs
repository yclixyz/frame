using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace Gugubao.Extensions
{
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]/[action]")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    public class CustomController : ControllerBase
    {
        protected readonly IMediator _mediator;

        public CustomController(IMediator mediator)
        {
            _mediator = mediator;
        }
    }
}
