using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Gugubao.Extensions
{
    /// <summary>
    /// 操作过过滤器 添加通用参数等
    /// </summary>
    public class AssignOperationExtensions : IOperationFilter
    {
        /// <summary>
        /// apply
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="context"></param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.ExternalDocs = new OpenApiExternalDocs
            {
                Description = "Api文档，仅用于开发",
                Url = new Uri("https://www.baidu.com")
            };

            //var hasAllowAttribute = context.MethodInfo.GetCustomAttributes(true)
            //              .Union(context.MethodInfo.DeclaringType.GetCustomAttributes(true))
            //              .OfType<AllowAnonymousAttribute>()
            //              .Any();

            //if (!hasAllowAttribute)
            //{
            //    operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
            //    operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });
            //}

            //operation.Description = "Api文档，仅用于开发";
        }
    }
}
