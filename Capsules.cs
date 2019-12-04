using System;
using System.Collections.Specialized;
using System.Web;

/// <summary>
/// AJAX Functionality
/// </summary>
namespace Ajax
{
    /// <summary>
    /// Categorizes every Capsule by its inheritance.
    /// Required due to their different AjaxHandler delegate signatures.
    /// </summary>
    public enum CapsuleType
    {
        /// <summary>
        /// Capsule inherited from InvocationCapsule
        /// </summary>
        Invocation,
        /// <summary>
        /// Capsule inherited from SubmissionCapsule
        /// </summary>
        Submission
    };

    /// <summary>
    /// Capsule Interface ( for easy use of Generics for Capsules e.g. mRegisteredCapsules Dictionary )
    /// </summary>
    public interface ICapsule
    {
        // Capsule Type Category
        CapsuleType Type
        {
            get;
        }
    }

    /// <summary>
    /// Capsule template for string transmission.
    /// Encapsulates a set of delegate/event for registration of serverside methods. 
    /// Valid Methods have a "string ( string )" Signature.<para>
    /// Override OnInvoke(string) to Handle Callbacks.</para>
    /// Use RegisteredMethod(string) to call the requested registered method.
    /// </summary>
    public abstract class InvocationCapsule : ICapsule
    {
        // Capsule Type Category
        public CapsuleType Type
        {
            get { return CapsuleType.Invocation; }
        }
        /// <summary>
        /// Specifies if the common rendering response should be interrupted and canceled. 
        /// Default is true.
        /// </summary>
        protected bool InterruptRendering = true;

        /// <summary>
        /// The response stream to write to
        /// </summary>
        protected HttpResponse Response;

        /// <summary>
        /// Delegate containing the method that shall be rised by Invoke event
        /// </summary>
        /// <param name="parameter">Information transmitted by Client</param>
        /// <returns>Information to transmit back to client</returns>
        public delegate string AjaxHandler(string parameter);

        /// <summary>
        /// Event that raises the registered Method
        /// </summary>
        public event AjaxHandler Invoke;

        /// <summary>
        /// Returns the internal AjaxHandler delegate to reflect the added method name
        /// </summary>
        /// <returns>AjaxHandler delegate</returns>
        internal AjaxHandler InternalDelegate
        {
            get { return Invoke; }
        }

        /// <summary>
        /// Calls the registered method requested by the client.
        /// </summary>
        /// <param name="parameter">Parameter to put into the registered method</param>
        /// <returns>Result value of the method</returns>
        protected string RegisteredMethod(string parameter)
        {
            return Invoke(parameter);
        }

        /// <summary>
        /// This will internally trigger the Event.
        /// </summary>
        /// <param name="parameter">Information transmitted by Client</param>
        /// <returns>Information to transmit back to client</returns>
        internal void RaiseInvocation(HttpResponse response, string parameter)
        {
            Response = response;
            if (InterruptRendering)
                response.Clear();
            // Call custom handling method and send response
            response.Write(OnInvoke(parameter));
            if (InterruptRendering)
                response.End();
        }

        /// <summary>
        /// Will be triggered on Callback. 
        /// Override this method to create your own Capsules.
        /// </summary>
        /// <param name="parameter">Information transmitted by Client</param>
        /// <returns>Information to transmit back to client</returns>
        protected abstract string OnInvoke(string parameter);
    }

    /// <summary>
    /// Capsule template for form transmission.
    /// Encapsulates a set of delegate/event for registration of serverside methods. 
    /// Valid Methods have a "string ( NameValueCollection )" Signature.<para>
    /// Override OnInvoke(string) to Handle Callbacks.</para>
    /// Use RegisteredMethod(string) to call the requested registered method.
    /// </summary>
    public abstract class SubmissionCapsule : ICapsule
    {
        // Capsule Type Category
        public CapsuleType Type
        {
            get { return CapsuleType.Submission; }
        }
        /// <summary>
        /// Specifies if the common rendering response should be interrupted and canceled. 
        /// Default is true.
        /// </summary>
        protected bool InterruptRendering = true;

        /// <summary>
        /// The response stream to write to
        /// </summary>
        protected HttpResponse Response;

        /// <summary>
        /// Delegate containing the method that shall be rised by Invoke event
        /// </summary>
        /// <param name="parameter">Information transmitted by Client</param>
        /// <returns>Information to transmit back to client</returns>
        public delegate string AjaxHandler(NameValueCollection parameter);

        /// <summary>
        /// Event that raises the registered Method
        /// </summary>
        public event AjaxHandler Invoke;

        /// <summary>
        /// Returns the internal AjaxHandler delegate to reflect the added method name
        /// </summary>
        /// <returns>AjaxHandler delegate</returns>
        internal AjaxHandler InternalDelegate
        {
            get { return Invoke; }
        }

        /// <summary>
        /// Calls the registered method requested by the client.
        /// </summary>
        /// <param name="parameter">Parameter to put into the registered method</param>
        /// <returns>Result value of the method</returns>
        protected string RegisteredMethod(NameValueCollection parameter)
        {
            return Invoke(parameter);
        }

        /// <summary>
        /// This will internally trigger the Event
        /// </summary>
        /// <param name="parameter">Information transmitted by Client</param>
        /// <returns>Information to transmit back to client</returns>
        internal void RaiseInvocation(HttpResponse response, NameValueCollection parameter)
        {
            Response = response;
            if (InterruptRendering)
                response.Clear();
            // Call custom handling method and send response
            response.Write(OnInvoke(parameter));
            if (InterruptRendering)
                response.End();
        }

        /// <summary>
        /// Will be triggered on Callback. 
        /// Override this method to create your own Capsules.
        /// </summary>
        /// <param name="parameter">Information transmitted by Client</param>
        /// <returns>Information to transmit back to client</returns>
        protected abstract string OnInvoke(NameValueCollection parameter);
    }

    /// <summary>
    /// Common Capsule for string transmission. 
    /// Triggers the registered method on Callback.
    /// </summary>
    public class CommonInvocation : InvocationCapsule
    {
        // Called by Invoke event
        protected override string OnInvoke(string parameter)
        {
            // Respond registered Method Response
            return this.RegisteredMethod(parameter);
        }
    }

    /// <summary>
    /// Common Capsule for form transmission. 
    /// Triggers the registered method on Callback.
    /// </summary>
    public class CommonSubmission : SubmissionCapsule
    {
        // Called by Invoke event
        protected override string OnInvoke(NameValueCollection parameter)
        {
            // Respond registered Method Response
            return this.RegisteredMethod(parameter);
        }
    }

}