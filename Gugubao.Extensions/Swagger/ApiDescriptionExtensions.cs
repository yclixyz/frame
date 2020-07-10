using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using System.Reflection;

namespace Gugubao.Extensions
{
    public static class ApiDescriptionExtensions
    {
        public static bool HasVersion(this ApiDescription apiDescription, string docName)
        {
            if (apiDescription.TryGetMethodInfo(out MethodInfo methodInfo))
            {
                var versions = methodInfo.GetCustomAttributes(true)
                      .Union(methodInfo.DeclaringType.GetCustomAttributes(true))
                      .Union(methodInfo.DeclaringType.BaseType.GetCustomAttributes(true))
                      .OfType<ApiVersionAttribute>()
                      .SelectMany(attr => attr.Versions);

                return versions.Any(v => $"v{v}" == docName);
            }

            return false;
        }
    }
}
