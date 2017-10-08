using System;
using System.IO;
using System.Text;
using System.Threading;
using Xunit;
using slackseNET;

namespace slackseNET.Tests.MegaHALSlackWrapper_Tests
{
    public class MegaHALSlackWrapper_TestInputCleaning
    {
        
        public MegaHALSlackWrapper_TestInputCleaning()
        {

        }
        [Fact]
        public void SlackStreamWriterRemovesNicks()
        {
            using(var outputStream = new MemoryStream())
            using(var slackStreamWriter = new MegaHALSlackWrapper.SlackStreamWriter(outputStream))
            {
                slackStreamWriter.WriteLine("<@29389KUK> This should only be this");
                slackStreamWriter.Flush();
                var result = Encoding.UTF8.GetString(outputStream.ToArray());
                Assert.Equal(result,"This should only be this\n");
            }
        }
        [Fact]
        public void SlackStreamReaderRemovesStartingGarbage()
        {
            var testString = "> - Hello there\n";
            var testStringBytes = Encoding.UTF8.GetBytes(testString);
            using(var inputStream = new MemoryStream(testStringBytes))
            {
                using(var slackStreamReader = new MegaHALSlackWrapper.SlackStreamReader(inputStream))
                {
                    var result = System.Threading.Tasks.Task.Run(() => slackStreamReader.ReadLineAsync());
                    result.Wait();
                    Assert.Equal(result.Result,"Hello there");
                }
            }

        }
    }
}
