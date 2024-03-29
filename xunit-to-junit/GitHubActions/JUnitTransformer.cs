﻿using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Xsl;

namespace InitialForce.GitHubActions.XUnitToJUnit
{
    /// <summary>
    /// Transforms a `xUnit.net v2 XML` test results file into a `JUnit` test results file.
    /// </summary>
    public static class JUnitTransformer
    {
        private static readonly XmlWriterSettings WriterSettings;
        private static readonly XslCompiledTransform XlsTransform;

        static JUnitTransformer()
        {
            XlsTransform = new XslCompiledTransform();
            XlsTransform.Load($"{AppContext.BaseDirectory}/JUnit.xslt");

            WriterSettings = XlsTransform.OutputSettings!.Clone();
            WriterSettings.Encoding = new UTF8Encoding(false);
        }

        /// <summary>
        /// Transforms a `xUnit.net v2 XML` test results file into a `JUnit` test results file.
        /// </summary>
        /// <param name="xUnitTestResultsFilePath">The `xUnit.net v2 XML` test results file path.</param>
        /// <param name="jUnitTestResultsFilePath">The `JUnit` test results file path, if the containing
        /// directory does not exist it will be created.</param>
        public static void Transform(string xUnitTestResultsFilePath, string jUnitTestResultsFilePath)
        {
            var jUnitTestResultsDirectory = Path.GetDirectoryName(jUnitTestResultsFilePath);

            if (!string.IsNullOrEmpty(jUnitTestResultsDirectory) && !Directory.Exists(jUnitTestResultsDirectory))
            {
                Directory.CreateDirectory(jUnitTestResultsDirectory);
            }

            using (var stream = new FileStream(jUnitTestResultsFilePath, FileMode.Create, FileAccess.Write))
            {
                Transform(xUnitTestResultsFilePath, stream);
            }
        }

        /// <summary>
        /// Transforms a `xUnit.net v2 XML` test results file into the `JUnit` format and write the result
        /// to a Stream.
        /// </summary>
        /// <param name="xUnitTestResultsFilePath">The `xUnit.net v2 XML` test results file path.</param>
        /// <param name="stream">The output Stream.</param>
        public static void Transform(string xUnitTestResultsFilePath, Stream stream)
        {
            using (var results = XmlWriter.Create(stream, WriterSettings))
            {
                XlsTransform.Transform(xUnitTestResultsFilePath, results);
            }
        }
    }
}
