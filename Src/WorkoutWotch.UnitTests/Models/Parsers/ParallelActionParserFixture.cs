﻿namespace WorkoutWotch.UnitTests.Models.Parsers
{
    using System;
    using System.Linq;
    using Kent.Boogaart.PCLMock;
    using Sprache;
    using WorkoutWotch.Models.Actions;
    using WorkoutWotch.Models.Parsers;
    using WorkoutWotch.UnitTests.Services.Container.Mocks;
    using Xunit;

    public class ParallelActionParserFixture
    {
        [Fact]
        public void get_parser_throws_if_indent_level_is_less_than_zero()
        {
            Assert.Throws<ArgumentException>(() => ParallelActionParser.GetParser(-1, new ContainerServiceMock()));
        }

        [Fact]
        public void get_parser_throws_if_container_service_is_null()
        {
            Assert.Throws<ArgumentNullException>(() => ParallelActionParser.GetParser(0, null));
        }

        [Theory]
        [InlineData(
            "Parallel:\n  * Say 'foo'\n  * Wait for 2s",
            0,
            new [] { typeof(SayAction), typeof(WaitAction) })]
        [InlineData(
            "Parallel:\n    * Say 'foo'\n    * Wait for 2s",
            1,
            new [] { typeof(SayAction), typeof(WaitAction) })]
        [InlineData(
            "PARALLEL:\n  * Say 'foo'\n  * Wait for 2s",
            0,
            new [] { typeof(SayAction), typeof(WaitAction) })]
        [InlineData(
            "Parallel:  \t  \n  * Say 'foo'\n  * Say 'bar'\n  * Say 'biz'",
            0,
            new [] { typeof(SayAction), typeof(SayAction), typeof(SayAction) })]
        [InlineData(
            "Parallel:\n\n\t\t  \n    \n\n  * Say 'foo'\n  * Say 'bar'\n  * Say 'biz'",
            0,
            new [] { typeof(SayAction), typeof(SayAction), typeof(SayAction) })]
        public void can_parse_valid_input(string input, int indentLevel, Type[] expectedActionTypes)
        {
            var result = ParallelActionParser
                .GetParser(indentLevel, new ContainerServiceMock(MockBehavior.Loose))
                .Parse(input);

            Assert.NotNull(result);
            Assert.True(result.Children.Select(x => x.GetType()).SequenceEqual(expectedActionTypes));
        }

        [Theory]
        [InlineData("Parallal:\n  * Say 'foo'", 0)]
        [InlineData("  Parallel:\n  * Say 'foo'", 0)]
        [InlineData("Parallel:\n", 0)]
        [InlineData("Parallel:\n  * Say 'foo'", 1)]
        public void cannot_parse_invalid_input(string input, int indentLevel)
        {
            var result = ParallelActionParser
                .GetParser(indentLevel, new ContainerServiceMock(MockBehavior.Loose))(new Input(input));
            Assert.False(result.WasSuccessful && result.Remainder.AtEnd);
        }
    }
}