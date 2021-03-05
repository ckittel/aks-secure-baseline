﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SimpleChainApi;
using SimpleChainApi.Services;

namespace RestAPIClient.Pages
{
    public class IndexModel : PageModel
    {
        private const string DEPTH = "DEPTH";

        private readonly ILogger<IndexModel> logger;

        private readonly IDependencyCallerService dependencyCallerService;

        public string FormatedResult { get; set; }
        public IndexModel(ILogger<IndexModel> logger, IConfiguration configuration, IDependencyCallerService dependencyCallerService)
        {
            Deep = string.IsNullOrWhiteSpace(configuration[DEPTH]) ? "0" : configuration[DEPTH];
            this.logger = logger;
            this.dependencyCallerService = dependencyCallerService;
        }

        [BindProperty]
        public string Deep { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                int deep = 0;
                int.TryParse(Deep, out deep);
                var response = await dependencyCallerService.ComputeDependenciesAsync(deep);
                StringBuilder stringBuilder = new StringBuilder();
                Format(response, stringBuilder, 0);
                FormatedResult = stringBuilder.ToString();
            }
            catch (ArgumentNullException uex)
            {
                logger.LogError("ArgumentNullException {uex}", uex);
            }
            catch (JsonReaderException jex)
            {
                logger.LogError("JsonReaderException {jex}", jex);
            }
            catch (Exception ex)
            {
                logger.LogError("Exception {uex}", ex);
            }
        }

        public void Format(DependencyResult dependencyResult, StringBuilder sb, int indent)
        {
            if (dependencyResult.ExternalDependencies.Any() || dependencyResult.SelfCalled.Any())
            {
                sb.AppendLine($"<br><div style='padding-left: {indent} em;'>");
                sb.AppendLine("<label>External dependencies: </label><br><ul>");
                foreach (var externalDependency in dependencyResult.ExternalDependencies)
                {
                    var color = externalDependency.Success ? "text-success" : "text-danger";
                    var status = externalDependency.StatusCode == 0 ? "Fail to connect" : $" StatusCode: {externalDependency.StatusCode}";
                    sb.Append($"<li><p class=\"{color}\"><i>{externalDependency.URI}</i><br>");
                    sb.Append(status);
                    sb.Append("</li></p>");
                }
                sb.Append("</ul>");

                sb.AppendLine("<label>Recursive dependencies: </label><br><ul>");
                foreach (var selfCalled in dependencyResult.SelfCalled)
                {
                    var color = selfCalled.Success ? "text-success" : "text-danger";
                    var status = selfCalled.StatusCode == 0 ? "Fail to connect" : $" StatusCode: {selfCalled.StatusCode}";
                    sb.Append($"<li><p class=\"{color}\"><i>{selfCalled.URI}</i><br>");
                    sb.Append(status);
                    Format(selfCalled.DependencyResult, sb, indent + 2);
                    sb.Append("</li></p>");
                }
                sb.Append("</ul>");

                sb.AppendLine("</div>");
            }
        }
    }
}
