﻿namespace WorkoutWotch.UnitTests.Models.Actions
{
    using System;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Threading;
    using Builders;
    using PCLMock;
    using WorkoutWotch.Models;
    using WorkoutWotch.UnitTests.Models.Mocks;
    using Xunit;

    public class SequenceActionFixture
    {
        [Fact]
        public void duration_is_zero_if_there_are_no_child_actions()
        {
            var sut = new SequenceActionBuilder()
                .Build();

            Assert.Equal(TimeSpan.Zero, sut.Duration);
        }

        [Fact]
        public void duration_is_calculated_as_the_sum_of_child_durations()
        {
            var action1 = new ActionMock();
            var action2 = new ActionMock();
            var action3 = new ActionMock();

            action1
                .When(x => x.Duration)
                .Return(TimeSpan.FromSeconds(10));

            action2
                .When(x => x.Duration)
                .Return(TimeSpan.FromSeconds(1));

            action3
                .When(x => x.Duration)
                .Return(TimeSpan.FromSeconds(7));

            var sut = new SequenceActionBuilder()
                .WithChild(action1)
                .WithChild(action2)
                .WithChild(action3)
                .Build();

            Assert.Equal(TimeSpan.FromSeconds(18), sut.Duration);
        }

        [Fact]
        public void execute_async_executes_each_child_action()
        {
            var action1 = new ActionMock(MockBehavior.Loose);
            var action2 = new ActionMock(MockBehavior.Loose);
            var action3 = new ActionMock(MockBehavior.Loose);
            var sut = new SequenceActionBuilder()
                .WithChild(action1)
                .WithChild(action2)
                .WithChild(action3)
                .Build();

            using (var context = new ExecutionContext())
            {
                sut.ExecuteAsync(context);

                action1
                    .Verify(x => x.ExecuteAsync(context))
                    .WasCalledExactlyOnce();

                action2
                    .Verify(x => x.ExecuteAsync(context))
                    .WasCalledExactlyOnce();

                action3
                    .Verify(x => x.ExecuteAsync(context))
                    .WasCalledExactlyOnce();
            }
        }

        [Fact]
        public void execute_async_executes_each_child_action_in_order()
        {
            var childCount = 10;
            var childExecutionOrder = new int[childCount];
            var executionOrder = 0;
            var childActions = Enumerable
                .Range(0, childCount)
                .Select(
                    (i, _) =>
                    {
                        var childAction = new ActionMock(MockBehavior.Loose);
                        childAction
                            .When(x => x.ExecuteAsync(It.IsAny<ExecutionContext>()))
                            .Do(() => childExecutionOrder[i] = Interlocked.Increment(ref executionOrder))
                            .Return(Observable.Return(Unit.Default));
                        return childAction;
                    })
                .ToList();
            
            var sut = new SequenceActionBuilder()
                .WithChildren(childActions)
                .Build();

            using (var context = new ExecutionContext())
            {
                sut.ExecuteAsync(context);

                for (var i = 0; i < childExecutionOrder.Length; ++i)
                {
                    Assert.Equal(i + 1, childExecutionOrder[i]);
                }
            }
        }

        [Fact]
        public void execute_async_skips_child_actions_that_are_shorter_than_the_skip_ahead()
        {
            var action1 = new ActionMock();
            var action2 = new ActionMock();
            var action3 = new ActionMock(MockBehavior.Loose);

            action1
                .When(x => x.ExecuteAsync(It.IsAny<ExecutionContext>()))
                .Throw();

            action2
                .When(x => x.ExecuteAsync(It.IsAny<ExecutionContext>()))
                .Throw();

            action1
                .When(x => x.Duration)
                .Return(TimeSpan.FromSeconds(3));

            action2
                .When(x => x.Duration)
                .Return(TimeSpan.FromSeconds(8));

            action3
                .When(x => x.Duration)
                .Return(TimeSpan.FromSeconds(2));

            var sut = new SequenceActionBuilder()
                .WithChild(action1)
                .WithChild(action2)
                .WithChild(action3)
                .Build();

            using (var context = new ExecutionContext(TimeSpan.FromSeconds(11)))
            {
                sut.ExecuteAsync(context);

                action3
                    .Verify(x => x.ExecuteAsync(context))
                    .WasCalledExactlyOnce();
            }
        }

        [Fact]
        public void execute_async_skips_child_actions_that_are_shorter_than_the_skip_ahead_even_if_the_context_is_paused()
        {
            var action1 = new ActionMock();
            var action2 = new ActionMock();
            var action3 = new ActionMock(MockBehavior.Loose);

            action1
                .When(x => x.ExecuteAsync(It.IsAny<ExecutionContext>()))
                .Throw();

            action2
                .When(x => x.ExecuteAsync(It.IsAny<ExecutionContext>()))
                .Throw();

            action1
                .When(x => x.Duration)
                .Return(TimeSpan.FromSeconds(3));

            action2
                .When(x => x.Duration)
                .Return(TimeSpan.FromSeconds(8));

            action3
                .When(x => x.Duration)
                .Return(TimeSpan.FromSeconds(2));

            var sut = new SequenceActionBuilder()
                .WithChild(action1)
                .WithChild(action2)
                .WithChild(action3)
                .Build();

            using (var context = new ExecutionContext(TimeSpan.FromSeconds(11)))
            {
                context.IsPaused = true;
                sut.ExecuteAsync(context);

                action3
                    .Verify(x => x.ExecuteAsync(context))
                    .WasCalledExactlyOnce();
            }
        }

        [Fact]
        public void execute_async_stops_executing_if_the_context_is_cancelled()
        {
            var action1 = new ActionMock(MockBehavior.Loose);
            var action2 = new ActionMock(MockBehavior.Loose);
            var action3 = new ActionMock(MockBehavior.Loose);
            var sut = new SequenceActionBuilder()
                .WithChild(action1)
                .WithChild(action2)
                .WithChild(action3)
                .Build();

            using (var context = new ExecutionContext())
            {
                action2
                    .When(x => x.ExecuteAsync(It.IsAny<ExecutionContext>()))
                    .Do(() => context.Cancel())
                    .Return(Observable.Return(Unit.Default));

                Assert.ThrowsAsync<OperationCanceledException>(async () => await sut.ExecuteAsync(context));

                action1
                    .Verify(x => x.ExecuteAsync(context))
                    .WasCalledExactlyOnce();

                action2
                    .Verify(x => x.ExecuteAsync(context))
                    .WasCalledExactlyOnce();

                action3
                    .Verify(x => x.ExecuteAsync(context))
                    .WasNotCalled();
            }
        }

        [Fact]
        public void execute_async_completes_even_if_there_are_no_child_actions()
        {
            var sut = new SequenceActionBuilder()
                .Build();

            var completed = false;
            sut
                .ExecuteAsync(new ExecutionContext())
                .Subscribe(_ => completed = true);

            Assert.True(completed);
        }
    }
}