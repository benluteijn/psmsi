﻿// The MIT License (MIT)
//
// Copyright (c) Microsoft Corporation
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Management.Automation;

namespace Microsoft.Tools.WindowsInstaller.PowerShell.Commands
{
    /// <summary>
    /// The data for actions to install a package.
    /// </summary>
    public class InstallCommandActionData
    {
        private long weight = 0;

        /// <summary>
        /// Gets or sets the package path for which the action is performed.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the ProductCode for which the action is performed.
        /// </summary>
        public string ProductCode { get; set; }

        /// <summary>
        /// Gets or sets the fully expanded command line to process when performing the action.
        /// </summary>
        public string CommandLine { get; set; }

        /// <summary>
        /// Gets an identifying name used for logging.
        /// </summary>
        internal virtual string LogName
        {
            get
            {
                if (!string.IsNullOrEmpty(this.Path))
                {
                    return System.IO.Path.GetFileNameWithoutExtension(this.Path);
                }
                else if (!string.IsNullOrEmpty(this.ProductCode))
                {
                    return this.ProductCode;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets or sets the weight of the package for progress reporting.
        /// </summary>
        /// <value>The calculated weight of the package, or the <see cref="PackageInfo.DefaultWeight"/> if not specified.</value>
        public long Weight
        {
            get
            {
                if (0 >= this.weight)
                {
                    return PackageInfo.DefaultWeight;
                }

                return this.weight;
            }

            set
            {
                this.weight = value;
            }
        }

        /// <summary>
        /// Creates an instance of an <see cref="InstallCommandActionData"/> class from the given file path.
        /// </summary>
        /// <typeparam name="T">The specific type of <see cref="InstallCommandActionData"/> to create.</typeparam>
        /// <param name="resolver">A <see cref="PathIntrinsics"/> object to resolve the file path.</param>
        /// <param name="file">A <see cref="PSObject"/> wrapping a file path.</param>
        /// <returns>An instance of an <see cref="InstallCommandActionData"/> class.</returns>
        public static T CreateActionData<T>(PathIntrinsics resolver, PSObject file)
            where T : InstallCommandActionData, new()
        {
            if (null == resolver)
            {
                throw new ArgumentNullException("resolver");
            }
            else if (null == file)
            {
                throw new ArgumentNullException("file");
            }

            var data = new T()
            {
                Path = resolver.GetUnresolvedProviderPathFromPSPath(file.Properties["PSPath"].Value as string),
            };

            return data;
        }

        /// <summary>
        /// Parses the arguments into the <see cref="CommandLine"/> property.
        /// </summary>
        /// <param name="args">The arguments to parse.</param>
        public void ParseCommandLine(string[] args)
        {
            if (null != args && 0 < args.Length)
            {
                this.CommandLine = string.Join(" ", args);
            }
        }

        /// <summary>
        /// Updates the <see cref="Weight"/> for progress reporting.
        /// </summary>
        public virtual void UpdateWeight()
        {
            if (!string.IsNullOrEmpty(this.Path))
            {
                this.Weight = PackageInfo.GetWeightFromPath(this.Path);
            }
            else if (!string.IsNullOrEmpty(this.ProductCode))
            {
                this.Weight = PackageInfo.GetWeightFromProductCode(this.ProductCode);
            }
        }
    }
}
