﻿namespace WorkoutWotch.UnitTests.ViewModels.Builders
{
    using System;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using Models.Builders;
    using PCLMock;
    using WorkoutWotch.Models;
    using WorkoutWotch.UnitTests.Services.Scheduler.Mocks;
    using WorkoutWotch.ViewModels;

    internal sealed class ExerciseViewModelBuilder : IBuilder
    {
        private IScheduler scheduler;
        private Exercise model;
        private IObservable<ExecutionContext> executionContext;

        public ExerciseViewModelBuilder()
        {
            this.scheduler = new SchedulerMock(MockBehavior.Loose);
            this.model = new ExerciseBuilder();
            this.executionContext = Observable.Never<ExecutionContext>();
        }

        public ExerciseViewModelBuilder WithSchedulerService(IScheduler scheduler) =>
            this.With(ref this.scheduler, scheduler);

        public ExerciseViewModelBuilder WithModel(Exercise model) =>
            this.With(ref this.model, model);

        public ExerciseViewModelBuilder WithExecutionContext(IObservable<ExecutionContext> executionContext) =>
            this.With(ref this.executionContext, executionContext);

        public ExerciseViewModelBuilder WithExecutionContext(ExecutionContext executionContext) =>
            this.WithExecutionContext(Observable.Return(executionContext));

        public ExerciseViewModel Build() =>
            new ExerciseViewModel(
                this.scheduler,
                this.model,
                this.executionContext);

        public static implicit operator ExerciseViewModel(ExerciseViewModelBuilder builder) =>
            builder.Build();
    }
}