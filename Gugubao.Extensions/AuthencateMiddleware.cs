using Gugubao.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace Gugubao.Extensions
{
    public class AuthencateMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthencateMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            await _next(context);

            // 处理http code=401 
            if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
            {
                var responseResult = new ResponseValue
                {
                    Success = false,
                    ErrorMsg = "未登陆，拒绝访问"
                };

                JsonSerializerOptions jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                jsonOptions.Converters.Add(new DatetimeJsonConverter());
                context.Response.Headers.Add("Content-type", "application/json;charset=UTF-8");
                await context.Response.WriteAsync(JsonSerializer.Serialize(responseResult, jsonOptions));
            }
        }
    }
}
