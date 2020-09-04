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
        private readonly string _projectName;

        public AssignOperationExtensions(string projectName)
        {
            _projectName = projectName;
        }

        /// <summary>
        /// apply
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="context"></param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.ExternalDocs = new OpenApiExternalDocs
            {
                Description = $"{_projectName}Api中心",
                Url = new Uri("https://www.lianghuiw.com")
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

            //operation.Description = "估小铺、估估宝、估豆卖菜系统类别分别为1,2,3";
        }
    }
}
