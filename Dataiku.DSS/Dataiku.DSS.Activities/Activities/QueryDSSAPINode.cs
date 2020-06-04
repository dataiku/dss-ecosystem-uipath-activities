using System;
using System.Activities;
using System.Threading;
using System.Threading.Tasks;
using Dataiku.DSS.Activities.Properties;
using UiPath.Shared.Activities;
using UiPath.Shared.Activities.Localization;

using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Drawing.Printing;

namespace Dataiku.DSS.Activities
{
    [LocalizedDisplayName(nameof(Resources.QueryDSSAPINode_DisplayName))]
    [LocalizedDescription(nameof(Resources.QueryDSSAPINode_Description))]
    public class QueryDSSAPINode : ContinuableAsyncCodeActivity
    {
        #region Properties

        /// <summary>
        /// If set, continue executing the remaining activities even if the current activity has failed.
        /// </summary>
        [LocalizedCategory(nameof(Resources.Common_Category))]
        [LocalizedDisplayName(nameof(Resources.ContinueOnError_DisplayName))]
        [LocalizedDescription(nameof(Resources.ContinueOnError_Description))]
        public override InArgument<bool> ContinueOnError { get; set; }

        [LocalizedCategory(nameof(Resources.Common_Category))]
        [LocalizedDisplayName(nameof(Resources.Timeout_DisplayName))]
        [LocalizedDescription(nameof(Resources.Timeout_Description))]
        public InArgument<int> TimeoutMS { get; set; } = 60000;

        [LocalizedDisplayName(nameof(Resources.QueryDSSAPINode_EndpointURL_DisplayName))]
        [LocalizedDescription(nameof(Resources.QueryDSSAPINode_EndpointURL_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> EndpointURL { get; set; }

        [LocalizedDisplayName(nameof(Resources.QueryDSSAPINode_APIKey_DisplayName))]
        [LocalizedDescription(nameof(Resources.QueryDSSAPINode_APIKey_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> APIKey { get; set; }

        [LocalizedDisplayName(nameof(Resources.QueryDSSAPINode_QueryData_DisplayName))]
        [LocalizedDescription(nameof(Resources.QueryDSSAPINode_QueryData_Description))]
        [LocalizedCategory(nameof(Resources.Input_Category))]
        public InArgument<string> QueryData { get; set; }


        [LocalizedDisplayName(nameof(Resources.QueryDSSAPINode_EndpointResult_DisplayName))]
        [LocalizedDescription(nameof(Resources.QueryDSSAPINode_EndpointResult_Description))]
        [LocalizedCategory(nameof(Resources.Output_Category))]
        public OutArgument<string> EndpointResult { get; set; }

        #endregion


        #region Constructors

        public QueryDSSAPINode()
        {
        }

        #endregion


        #region Protected Methods

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            if (EndpointURL == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(EndpointURL)));
            if (APIKey == null) metadata.AddValidationError(string.Format(Resources.ValidationValue_Error, nameof(APIKey)));

            base.CacheMetadata(metadata);
        }

        protected override async Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken)
        {
            // Inputs
            var timeout = TimeoutMS.Get(context);

            /* I'm guessing these inputs are useless! They aren't passed to the timeout exec.
            var endpointURL = EndpointURL.Get(context);
            var apiKey = APIKey.Get(context);
            var queryData = QueryData.Get(context);
            */

            // Set a timeout on the execution
            var task = ExecuteWithTimeout(context, cancellationToken);
            if (await Task.WhenAny(task, Task.Delay(timeout, cancellationToken)) != task) throw new TimeoutException(Resources.Timeout_Error);


            // Prepare result for return
            // probably task.Result and finalResult are actually the same and this is useless (?)
            JObject jsonResponse = JObject.Parse(task.Result);
            string finalResult = jsonResponse.ToString();


            // Outputs
            return (ctx) => {
                EndpointResult.Set(ctx, finalResult);
            };
        }

        private async Task<string> ExecuteWithTimeout(AsyncCodeActivityContext context, CancellationToken cancellationToken = default)
        {
            ///////////////////////////
            
            // Get inputs from context 
            var endpointURL = EndpointURL.Get(context);
            var apiKey = APIKey.Get(context);
            var queryData = QueryData.Get(context);

            /* Build request body (feature dictionary) */
            // -> here we assume that the queryData is a json string with keys corresponding to features
            JArray jsonDataArray = JArray.Parse(queryData);
            JObject jsonData = (JObject) jsonDataArray.First;
            // -> we build the feature dictionary just by putting all keys into it
            var jsonQuery = new JObject();
            jsonQuery.Add("features", jsonData);
            // -> StringContent is required for HTTP POST call
            var stringContent = new StringContent(jsonQuery.ToString());

            /* Initialize HttpClient object */
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(endpointURL);

            // -> Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            // -> Handle authentication
            var authToken = Encoding.ASCII.GetBytes(apiKey + ":"); // don't forget the colon!!
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(authToken));


            /* Post query to DSS API Node */
            HttpResponseMessage response = await client.PostAsync(String.Empty, stringContent);
            response.EnsureSuccessStatusCode();
            var resp = await response.Content.ReadAsStringAsync();

            // Dispose once all HttpClient calls are complete. This is not necessary if the containing object will be disposed of; for example in this case the HttpClient instance will be disposed automatically when the application terminates so the following call is superfluous.
            client.Dispose();

            // Outputs
            return resp;
        }

#endregion
    }
}

