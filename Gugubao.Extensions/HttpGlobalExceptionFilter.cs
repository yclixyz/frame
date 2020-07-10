using Gugubao.Utility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Gugubao.Extensions
{
    public class HttpGlobalExceptionFilter : IExceptionFilter
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<HttpGlobalExceptionFilter> _logger;

        public HttpGlobalExceptionFilter(IWebHostEnvironment env, ILogger<HttpGlobalExceptionFilter> logger)
        {
            _env = env;
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            try
            {
                //记录日志
                _logger.LogError(context.Exception.ToString());

                //状态码
                if (context.Exception is UnauthorizedAccessException)
                {
                    context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                }
                else if (context.Exception is DomainException)
                {
                    context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }
                else
                {
                    context.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                }

                context.HttpContext.Response.ContentType = context.HttpContext.Request.Headers["Accept"];

                context.HttpContext.Response.ContentType = "application/json";

                var responseResult = new ResponseValue
                {
                    Success = false
                };

                if (_env.EnvironmentName == Environments.Development || context.Exception is DomainException)
                {
                    responseResult.ErrorMsg = (context.Exception.GetBaseException().Message);
                }
                else
                {
                    responseResult.ErrorMsg = "详细错误请联系管理员";
                }

                JsonSerializerOptions jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                jsonOptions.Converters.Add(new DatetimeJsonConverter());

                context.Result = new JsonResult(JsonSerializer.Serialize(responseResult, jsonOptions));

                context.ExceptionHandled = true;
            }
            catch (Exception e)
            {
                _logger.LogError("异常捕获失败", e.ToString());
            }
        }
    }
}
