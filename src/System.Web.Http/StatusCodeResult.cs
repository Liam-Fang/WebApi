﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Properties;

namespace System.Web.Http
{
    /// <summary>Represents an action result that returns a specified HTTP status code.</summary>
    public class StatusCodeResult : IHttpActionResult
    {
        private readonly HttpStatusCode _statusCode;
        private readonly ILazyDependencyProvider _dependencies;

        /// <summary>Initializes a new instance of the <see cref="StatusCodeResult"/> class.</summary>
        /// <param name="statusCode">The HTTP status code for the response message.</param>
        /// <param name="request">The request message which led to this result.</param>
        public StatusCodeResult(HttpStatusCode statusCode, HttpRequestMessage request)
            : this(statusCode, new DirectDependencyProvider(request))
        {
        }

        internal StatusCodeResult(HttpStatusCode statusCode, ApiController controller)
            : this(statusCode, new ApiControllerDependencyProvider(controller))
        {
        }

        private StatusCodeResult(HttpStatusCode statusCode, ILazyDependencyProvider dependencies)
        {
            Contract.Assert(dependencies != null);

            _statusCode = statusCode;
            _dependencies = dependencies;
        }

        /// <summary>Gets the HTTP status code for the response message.</summary>
        public HttpStatusCode StatusCode
        {
            get { return _statusCode; }
        }

        /// <summary>Gets the request message which led to this result.</summary>
        public HttpRequestMessage Request
        {
            get { return _dependencies.Request; }
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Execute());
        }

        private HttpResponseMessage Execute()
        {
            HttpResponseMessage mutableResponse = new HttpResponseMessage(_statusCode);
            HttpResponseMessage response = null;

            try
            {
                mutableResponse.RequestMessage = _dependencies.Request;

                response = mutableResponse;
                mutableResponse = null;
            }
            finally
            {
                if (mutableResponse != null)
                {
                    mutableResponse.Dispose();
                }
            }

            return response;
        }

        private interface ILazyDependencyProvider
        {
            HttpRequestMessage Request { get; }
        }

        private class DirectDependencyProvider : ILazyDependencyProvider
        {
            private readonly HttpRequestMessage _request;

            public DirectDependencyProvider(HttpRequestMessage request)
            {
                if (request == null)
                {
                    throw new ArgumentNullException("request");
                }

                _request = request;
            }

            public HttpRequestMessage Request
            {
                get { return _request; }
            }
        }

        private class ApiControllerDependencyProvider : ILazyDependencyProvider
        {
            private readonly ApiController _controller;

            private ILazyDependencyProvider _resolved;

            public ApiControllerDependencyProvider(ApiController controller)
            {
                if (controller == null)
                {
                    throw new ArgumentNullException("controller");
                }

                _controller = controller;
            }

            public HttpRequestMessage Request
            {
                get
                {
                    EnsureResolved();
                    return _resolved.Request;
                }
            }

            private void EnsureResolved()
            {
                if (_resolved == null)
                {
                    HttpRequestMessage request = _controller.Request;

                    if (request == null)
                    {
                        throw new InvalidOperationException(SRResources.ApiControllerResult_MustNotBeNull);
                    }

                    _resolved = new DirectDependencyProvider(request);
                }
            }
        }
    }
}