﻿namespace WorkoutWotch.UnitTests.Models.Builders
{
    using System.Collections.Generic;
    using PCLMock;
    using WorkoutWotch.Models;
    using WorkoutWotch.Services.Contracts.Logger;
    using WorkoutWotch.UnitTests.Services.Logger.Mocks;

    internal sealed class ExerciseProgramBuilder
    {
        private readonly IList<Exercise> exercises;
        private ILoggerService loggerService;
        private string name;

        public ExerciseProgramBuilder()
        {
            this.exercises = new List<Exercise>();
            this.loggerService = new LoggerServiceMock(MockBehavior.Loose);
            this.name = "Name";
        }

        public ExerciseProgramBuilder WithLoggerService(ILoggerService loggerService)
        {
            this.loggerService = loggerService;
            return this;
        }

        public ExerciseProgramBuilder WithName(string name)
        {
            this.name = name;
            return this;
        }

        public ExerciseProgramBuilder AddExercise(Exercise exercise)
        {
            this.exercises.Add(exercise);
            return this;
        }

        public ExerciseProgramBuilder AddExercises(IEnumerable<Exercise> exercises)
        {
            foreach (var exercise in exercises)
            {
                this.exercises.Add(exercise);
            }

            return this;
        }

        public ExerciseProgram Build() =>
            new ExerciseProgram(
                this.loggerService,
                this.name,
                this.exercises);

        public static implicit operator ExerciseProgram(ExerciseProgramBuilder builder) =>
            builder.Build();
    }
}