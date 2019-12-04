using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI.WebControls;
using System.Reflection;

/// <summary>
/// AJAX Functionality
/// </summary>
namespace Ajax
{
    /// <summary>
    /// Ajax communication Pipeline
    /// <para>Allows registering of Methods via events which can be called by Client.</para>
    /// </summary>
    public class AjaxInvocation : WebControl
    {
        /// <summary>
        /// Holds the registered Capsules that will handle callbacks.
        /// </summary>
        private Dictionary<string, ICapsule> mRegisteredCapsules = new Dictionary<string, ICapsule>();

        public bool IsCallback
        {
            get
            {
                string nRequest = this.Page.Request.Params["AjaxXmlHttp"];
                return (!string.IsNullOrEmpty(nRequest));
            }
        }

        /// <summary>
        /// <para>Analyses the current Request.</para>
        /// <para>Raises the OnInvoke event of the desired capsule on Callback.</para>
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            // Get identifying AjaxRequestToken
            if (IsCallback)
            {
                string nRequest = this.Page.Request.Params["AjaxXmlHttp"];
                // Check if requested Capsule exists
                if (mRegisteredCapsules.ContainsKey(nRequest))
                {
                    // Callback identified and corresponding Capsule found -> Hit event and run back to Client
                    ICapsule nCap = mRegisteredCapsules[nRequest];
                    if (nCap.Type == CapsuleType.Invocation)
                    {
                        // InvocationCapsule
                        string nParam = this.Page.Request.Params["AjaxParam"];
                        ((InvocationCapsule)nCap).RaiseInvocation(this.Page.Response, nParam);
                    }
                    else if (nCap.Type == CapsuleType.Submission)
                    {
                        // SubmissionCapsule
                        ((SubmissionCapsule)nCap).RaiseInvocation(this.Page.Response, this.Page.Request.Params);
                    }
                }
            }
        }

        /// <summary>
        /// Registers a Capsule to handle callbacks
        /// </summary>
        /// <param name="capsule">Capsule to register</param>
        public void RegisterCapsule(ICapsule capsule)
        {
            // Use Methodname as Key
            string nKey = string.Empty;
            if (capsule.Type == CapsuleType.Invocation)
            {
                // InvocationCapsule
                nKey = ((InvocationCapsule)capsule).InternalDelegate.Method.Name;
            }
            else if (capsule.Type == CapsuleType.Submission)
            {
                // SubmissionCapsule
                nKey = ((SubmissionCapsule)capsule).InternalDelegate.Method.Name;
            }
            // Register Capsule
            if (mRegisteredCapsules.ContainsKey(nKey))
            {
                // Key already contained -> overwrite old Capsule
                mRegisteredCapsules[nKey] = capsule;
            }
            else
            {
                // Add new Capsule
                mRegisteredCapsules.Add(nKey, capsule);
            }
        }

        /// <summary>
        /// If the current Request wasn't a callback, the Control will Render itself as Javascript.<para>
        /// </summary>
        /// <param name="writer">Stream to render Control to</param>
        protected override void Render(System.Web.UI.HtmlTextWriter writer)
        {
            string nOutput = @"
<script type=""text/javascript"">
if( !AjaxInvocation )
{
    // The AjaxInvocation Object
    // Delivers client AJAX functionality
    var AjaxInvocation = new Object();
    // Object for internal functionality
    AjaxInvocation.InternalAjaxInvocation = {
        // Create a new XmlHttpRequest Object
        CreateXmlHttpRequest: function()
        {
            var nXmlHttp = null;
            // Mozilla, Opera, Safari, Internet Explorer 7
            if (typeof XMLHttpRequest != 'undefined') 
            {
                nXmlHttp = new XMLHttpRequest();
            }
            if (!nXmlHttp) 
            {
                // Internet Explorer 6 and older
                try 
                {
                    nXmlHttp  = new ActiveXObject(""Msxml2.XMLHTTP"");
                } 
                catch(e) 
                {
                    try 
                    {
                        nXmlHttp  = new ActiveXObject(""Microsoft.XMLHTTP"");
                    } 
                    catch(e) 
                    {
                        nXmlHttp  = null;
                    }
                }
            }
            return nXmlHttp;
        },
        // Send CallBack
        InvokeCallBack:function(query, onResponse)
        {        
            var nXmlHttp = this.CreateXmlHttpRequest();
            if (nXmlHttp) 
            {
                // Prepare callback
                var nIsAsync = (onResponse != null);
                nXmlHttp.open('POST', document.URL, nIsAsync);
                nXmlHttp.setRequestHeader('Content-Type','application/x-www-form-urlencoded');
                nXmlHttp.onreadystatechange = function () 
                {   
                    // wait for response
                    if (nXmlHttp.readyState != 4) 
                    {
                        // wait for Response
                        return false;
                    }
                    else if( nIsAsync )
                    {
                        // Response recieved and asynchronous mode
                        if (nXmlHttp.status == 200) 
                        {
                            // Successful Response -> call Handler
                            onResponse(nXmlHttp.responseText, nXmlHttp.responseXml);
                        }
                        else if( nXmlHttp.status == 0 )
                        {
                            // Request aborted
                            AjaxInvocation.OnAbort();
                        }
                        else
                        {
                            // Error Response
                            AjaxInvocation.OnError( nXmlHttp.responseText, nXmlHttp.status, nXmlHttp.statusText );
                        }
                    }
                };
                // invoke callback
                nXmlHttp.send(query);
                // Response for Asyncronous Communication
                if( !nIsAsync ) return nXmlHttp.responseText;
            }
            else
            {
                // Failed to create XmlHttpObject
                AjaxInvocation.OnInitFailure();
            }
        }
    };
    // Handles XmlHttpObject creation failure.
    AjaxInvocation.OnInitFailure = function()
    {
        alert(""Your Browser doesn't support AJAX! Please use a newer one."");
        return null;
    };
    // Handles aborted requests
    AjaxInvocation.OnAbort = function()
    {
        alert(""An unexpected error occured while processing your request.\nThe request has been aborted."");
        return null;
    };
    // Handles errors (like 404).
    AjaxInvocation.OnError = function( responseText, status, statusText )
    {
        alert(""An unexpected error occured while processing your request.\nCode: "" + status + ""\nReason: "" + statusText);
        return null;
    };
    // Initiate callback for an InvocationCapsule
    AjaxInvocation.SummonServer = function(serverMethod, param, onResponse)
    {
        // Create Query
        var nQuery = 'AjaxXmlHttp=' + serverMethod; 
        if( param )
        {
            // Encode Parameter
            nQuery += '&AjaxParam=' + encodeURIComponent(param);
        }
        return this.InternalAjaxInvocation.InvokeCallBack(nQuery, onResponse);
    };
    // Initiate callback for an SubmissionCapsule
    AjaxInvocation.SummonSubmit = function(serverMethod, form, onResponse, includeViewState)
    {
        // Create Query
        var nQuery = 'AjaxXmlHttp=' + serverMethod; 
        if(form)
        {
            // Parse Form
            for( iElem = 0; iElem < form.length; iElem++ )
            {
                var nElem = form.elements[iElem];
                var nElemName = nElem.name; 
                if( nElemName == null || nElemName == '' ) { nElemName = nElem.id };
                if( nElemName != '__VIEWSTATE' || includeViewState == true )
                {
                    if( (nElem.type!='radio' && nElem.type!='checkbox' && nElem.type!='button') || nElem.checked == true )
                    {
                        nQuery += '&' + encodeURIComponent(nElemName) + '=' + encodeURIComponent(nElem.value);
                    }
                }
            }
        }
        return this.InternalAjaxInvocation.InvokeCallBack(nQuery, onResponse);
    };
}";
            // Create Client Versions of registered Methods
            foreach (KeyValuePair<string, ICapsule> iItem in mRegisteredCapsules)
            {
                if (iItem.Value.Type == CapsuleType.Submission)
                {
                    // SubmissionCapsule
                    nOutput += @"
// Remote method
AjaxInvocation." + iItem.Key + @" = function(form, onResponse, includeViewState)
{
    return AjaxInvocation.SummonSubmit(""" + iItem.Key + @""", form, onResponse, includeViewState);
};";
                }
                else if (iItem.Value.Type == CapsuleType.Invocation)
                {
                    // InvocationCapsule
                    nOutput += @"
// Remote method
AjaxInvocation." + iItem.Key + @" = function(param, onResponse)
{
    return AjaxInvocation.SummonServer(""" + iItem.Key + @""", param, onResponse);
};";
                }
            }
            nOutput += @"
</script>";
            // Render to Output Stream
            writer.Write(nOutput);
        }
    }
}
