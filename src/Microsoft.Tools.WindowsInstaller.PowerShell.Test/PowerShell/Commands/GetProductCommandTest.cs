﻿// Copyright (C) Microsoft Corporation. All rights reserved.
//
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.

using Microsoft.Deployment.WindowsInstaller;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Microsoft.Tools.WindowsInstaller.PowerShell.Commands
{
    /// <summary>
    /// Unit and functional tests for <see cref="GetProductCommand"/>.
    ///</summary>
    [TestClass]
    public class GetProductCommandTest : TestBase
    {
        [TestMethod]
        public void EnumerateProductsInDefaultContext()
        {
            var expected = new List<string>();
            expected.Add("{89F4137D-6C26-4A84-BDB8-2E5A4BB71E00}");
            expected.Add("{877EF582-78AF-4D84-888B-167FDC3BCC11}");
            expected.Add("{B4EA7821-1AC1-41B5-8021-A2FC77D1B7B7}");

            using (var p = CreatePipeline(@"get-msiproductinfo"))
            {
                using (OverrideRegistry())
                {
                    var objs = p.Invoke();

                    var actual = new List<string>(objs.Count);
                    foreach (var obj in objs)
                    {
                        actual.Add(obj.GetPropertyValue<string>("ProductCode"));
                    }

                    Assert.AreEqual<int>(expected.Count, objs.Count);
                    CollectionAssert.AreEquivalent(expected, actual);
                }
            }
        }

        [TestMethod]
        public void EnumerateProducts()
        {
            var products = new List<string>();
            products.Add("{89F4137D-6C26-4A84-BDB8-2E5A4BB71E00}");

            using (var p = CreatePipeline(@"get-msiproductinfo -context Machine"))
            {
                using (OverrideRegistry())
                {
                    var objs = p.Invoke();

                    var actual = new List<string>(objs.Count);
                    foreach (var obj in objs)
                    {
                        actual.Add(obj.Properties["ProductCode"].Value as string);
                    }

                    Assert.AreEqual<int>(products.Count, objs.Count);
                    CollectionAssert.AreEquivalent(products, actual);
                }
            }
        }

        [TestMethod]
        public void EnumerateUserUnmanagedProducts()
        {
            List<string> expected = new List<string>();
            expected.Add("{877EF582-78AF-4D84-888B-167FDC3BCC11}");
            expected.Add("{B4EA7821-1AC1-41B5-8021-A2FC77D1B7B7}");

            using (var p = CreatePipeline(string.Format(@"get-msiproductinfo -installcontext userunmanaged -usersid '{0}'", CurrentSID)))
            {
                using (OverrideRegistry())
                {
                    var objs = p.Invoke();

                    var actual = new List<string>(objs.Count);
                    foreach (var obj in objs)
                    {
                        actual.Add(obj.Properties["ProductCode"].Value as string);
                    }

                    Assert.AreEqual<int>(expected.Count, objs.Count);
                    CollectionAssert.AreEquivalent(expected, actual);
                }
            }
        }

        [TestMethod]
        public void EnumerateNamedProducts()
        {
            // Use two strings that will match the same product; make sure only one product is returned.
            using (var p = CreatePipeline(@"get-msiproductinfo -name Silver*, *Light"))
            {
                using (OverrideRegistry())
                {
                    var objs = p.Invoke();
                    Assert.AreEqual<int>(1, objs.Count);
                    Assert.AreEqual<string>("{89F4137D-6C26-4A84-BDB8-2E5A4BB71E00}", objs[0].GetPropertyValue<string>("ProductCode"));
                }
            }
        }

        [TestMethod]
        public void ProductCodeTest()
        {
            // Finally invoke the cmdlet for a single product.
            using (var p = CreatePipeline(@"get-msiproductinfo -productcode '{89F4137D-6C26-4A84-BDB8-2E5A4BB71E00}'"))
            {
                using (OverrideRegistry())
                {
                    var objs = p.Invoke();

                    Assert.AreEqual<int>(1, objs.Count);
                    Assert.AreEqual<string>("{89F4137D-6C26-4A84-BDB8-2E5A4BB71E00}", objs[0].GetPropertyValue<string>("ProductCode"));
                }
            }
        }

        [TestMethod]
        public void UserSidTest()
        {
            var cmdlet = new GetProductCommand();

            // Test the default.
            Assert.AreEqual<string>(null, cmdlet.UserSid);

            // Test that what we explicitly set is returned.
            cmdlet.UserSid = "S-1-5-21-2127521184-1604012920-1887927527-2039434";
            Assert.AreEqual<string>("S-1-5-21-2127521184-1604012920-1887927527-2039434", cmdlet.UserSid);
        }

        [TestMethod]
        public void InstallContextTest()
        {
            // Test that None is not supported.
            var cmdlet = new GetProductCommand();
            ExceptionAssert.Throws<ArgumentException>(() =>
            {
                cmdlet.UserContext = UserContexts.None;
            });

            var expected = new List<string>();
            expected.Add("{877EF582-78AF-4D84-888B-167FDC3BCC11}");
            expected.Add("{B4EA7821-1AC1-41B5-8021-A2FC77D1B7B7}");

            // Test that "Context" is a supported alias.
            using (var p = CreatePipeline(string.Format(@"get-msiproductinfo -context userunmanaged -usersid '{0}'", CurrentSID)))
            {
                using (OverrideRegistry())
                {
                    var objs = p.Invoke();

                    var actual = new List<string>(objs.Count);
                    foreach (var obj in objs)
                    {
                        actual.Add(obj.GetPropertyValue<string>("ProductCode"));
                    }

                    Assert.AreEqual<int>(expected.Count, objs.Count);
                    CollectionAssert.AreEquivalent(expected, actual);
                }
            }
        }

        [TestMethod]
        public void EveryoneTest()
        {
            var cmdlet = new GetProductCommand();

            // Test that the default is false / not present.
            Assert.AreEqual<bool>(false, cmdlet.Everyone);
            Assert.AreEqual<bool>(false, cmdlet.Everyone.IsPresent);

            // Test that we can set it to true.
            cmdlet.Everyone = true;
            Assert.AreEqual<bool>(true, cmdlet.Everyone);
            Assert.AreEqual<string>(NativeMethods.World, cmdlet.UserSid);

            // Test that explicitly setting it to false nullifies the UserSid.
            cmdlet.Everyone = false;
            Assert.AreEqual<bool>(false, cmdlet.Everyone);
            Assert.AreEqual<string>(null, cmdlet.UserSid);
        }

        [TestMethod]
        [WorkItem(9464)]
        public void GetProductChainedExecution()
        {
            using (var p = CreatePipeline(@"get-msiproductinfo '{89F4137D-6C26-4A84-BDB8-2E5A4BB71E00}' | get-msiproductinfo"))
            {
                using (OverrideRegistry())
                {
                    var objs = p.Invoke();

                    Assert.AreEqual(1, objs.Count);
                    Assert.AreEqual<string>("{89F4137D-6C26-4A84-BDB8-2E5A4BB71E00}", objs[0].GetPropertyValue<string>("ProductCode"));
                }
            }
        }
    }
}
