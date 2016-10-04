﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Core.Test
{
    public class AcceptedResultTests
    {
        public static TheoryData<object> ValuesData
        {
            get
            {
                return new TheoryData<object>
                {
                    null,
                    "Test string",
                    new object(),
                };
            }
        }

        [Theory]
        [MemberData(nameof(ValuesData))]
        public void Constructor_InitializesStatusCodeAndValue(object value)
        {
            // Arrange & Act
            var result = new AcceptedResult("testlocation", value);

            // Assert
            Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
            Assert.Same(value, result.Value);
        }

        [Theory]
        [MemberData(nameof(ValuesData))]
        public async Task ExecuteResultAsync_SetsStatusCodeAndValue(object value)
        {
            // Arrange
            var location = "/test/";
            var httpContext = GetHttpContext();
            var actionContext = GetActionContext(httpContext);
            var result = new AcceptedResult(location, value);

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal(StatusCodes.Status202Accepted, httpContext.Response.StatusCode);
            Assert.Same(value, result.Value);
        }

        [Fact]
        public async Task ExecuteResultAsync_SetsStatusCodeAndLocationHeader()
        {
            // Arrange
            var location = "/test/";
            var httpContext = GetHttpContext();
            var actionContext = GetActionContext(httpContext);
            var result = new AcceptedResult(location, "testInput");

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal(StatusCodes.Status202Accepted, httpContext.Response.StatusCode);
            Assert.Equal(location, httpContext.Response.Headers["Location"]);
        }

        [Fact]
        public async Task ExecuteResultAsync_OverwritesLocationHeader()
        {
            // Arrange
            var location = "/test/";
            var httpContext = GetHttpContext();
            var actionContext = GetActionContext(httpContext);
            httpContext.Response.Headers["Location"] = "/different/location/";
            var result = new AcceptedResult(location, "testInput");

            // Act
            await result.ExecuteResultAsync(actionContext);

            // Assert
            Assert.Equal(StatusCodes.Status202Accepted, httpContext.Response.StatusCode);
            Assert.Equal(location, httpContext.Response.Headers["Location"]);
        }

        private static ActionContext GetActionContext(HttpContext httpContext)
        {
            var routeData = new RouteData();
            routeData.Routers.Add(Mock.Of<IRouter>());
            return new ActionContext(
                httpContext,
                routeData,
                new ActionDescriptor());
        }

        private static HttpContext GetHttpContext()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = CreateServices();
            return httpContext;
        }

        private static IServiceProvider CreateServices()
        {
            var options = new TestOptionsManager<MvcOptions>();
            options.Value.OutputFormatters.Add(new StringOutputFormatter());
            options.Value.OutputFormatters.Add(new JsonOutputFormatter(
                new JsonSerializerSettings(),
                ArrayPool<char>.Shared));

            var services = new ServiceCollection();
            services.AddSingleton(new ObjectResultExecutor(
                options,
                new TestHttpResponseStreamWriterFactory(),
                NullLoggerFactory.Instance));

            return services.BuildServiceProvider();
        }
    }
}
