﻿using System;
using System.Collections.Generic;
using WireMock.Admin.Mappings;
using WireMock.Util;

namespace WireMock.Admin.Requests
{
    /// <summary>
    /// RequestMessage Model
    /// </summary>
    public class LogRequestModel
    {
        /// <summary>
        /// Gets the DateTime.
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// Gets or sets the Path.
        /// </summary>
        /// <value>
        /// The Path.
        /// </value>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the absolete URL.
        /// </summary>
        /// <value>
        /// The absolute URL.
        /// </value>
        public string AbsoluteUrl { get; set; }

        /// <summary>
        /// Gets the query.
        /// </summary>
        public IDictionary<string, WireMockList<string>> Query { get; set; }

        /// <summary>
        /// Gets or sets the method.
        /// </summary>
        /// <value>
        /// The method.
        /// </value>
        public string Method { get; set; }

        /// <summary>
        /// Gets or sets the Headers.
        /// </summary>
        /// <value>
        /// The Headers.
        /// </value>
        public IDictionary<string, string> Headers { get; set; }

        /// <summary>
        /// Gets or sets the Cookies.
        /// </summary>
        /// <value>
        /// The Cookies.
        /// </value>
        public IDictionary<string, string> Cookies { get; set; }

        /// <summary>
        /// Gets or sets the body.
        /// </summary>
        /// <value>
        /// The body.
        /// </value>
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets the body encoding.
        /// </summary>
        /// <value>
        /// The body encoding.
        /// </value>
        public EncodingModel BodyEncoding { get; set; }
    }
}