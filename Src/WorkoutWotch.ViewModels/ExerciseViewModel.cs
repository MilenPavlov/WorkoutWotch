namespace WorkoutWotch.ViewModels
{
    using System;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using ReactiveUI;
    using WorkoutWotch.Models;
    using WorkoutWotch.Services.Contracts.Scheduler;
    using WorkoutWotch.Utility;

    public sealed class ExerciseViewModel : DisposableReactiveObject
    {
        private readonly CompositeDisposable disposables;
        private readonly Exercise model;
        private ExecutionContext executionContext;
        private TimeSpan progressTimeSpan;
        private bool isActive;
        private double progress;

        public ExerciseViewModel(ISchedulerService schedulerService, Exercise model, IObservable<ExecutionContext> executionContext)
        {
            Ensure.ArgumentNotNull(schedulerService, nameof(schedulerService));
            Ensure.ArgumentNotNull(model, nameof(model));
            Ensure.ArgumentNotNull(executionContext, nameof(executionContext));

            this.disposables = new CompositeDisposable();
            this.model = model;

            executionContext
                .ObserveOn(schedulerService.MainScheduler)
                .Subscribe(x => this.ExecutionContext = x)
                .AddTo(this.disposables);

            Observable
                .CombineLatest(
                    this
                        .WhenAnyValue(x => x.ExecutionContext)
                        .Select(ec => ec == null ? Observable.Never<TimeSpan>() : ec.WhenAnyValue(x => x.SkipAhead))
                        .Switch(),
                    this
                        .WhenAnyValue(x => x.ExecutionContext)
                        .Select(ec => ec == null ? Observable.Never<Exercise>() : ec.WhenAnyValue(x => x.CurrentExercise))
                        .Switch(),
                    (skip, current) => skip == TimeSpan.Zero && current == this.model)
                .ObserveOn(schedulerService.MainScheduler)
                .Subscribe(x => this.IsActive = x)
                .AddTo(this.disposables);

            this
                .WhenAnyValue(x => x.ExecutionContext)
                .Select(
                    ec =>
                        ec == null
                            ? Observable.Return(TimeSpan.Zero)
                            : ec
                                .WhenAnyValue(x => x.CurrentExerciseProgress)
                                .Where(_ => ec.CurrentExercise == this.model)
                                .StartWith(TimeSpan.Zero))
                .Switch()
                .ObserveOn(schedulerService.MainScheduler)
                .Subscribe(x => this.ProgressTimeSpan = x)
                .AddTo(this.disposables);

            this
                .WhenAny(
                    x => x.Duration,
                    x => x.ProgressTimeSpan,
                    (duration, progressTimeSpan) => progressTimeSpan.Value.TotalMilliseconds / duration.Value.TotalMilliseconds)
                .Select(progressRatio => double.IsNaN(progressRatio) || double.IsInfinity(progressRatio) ? 0d : progressRatio)
                .Select(progressRatio => Math.Min(1d, progressRatio))
                .Select(progressRatio => Math.Max(0d, progressRatio))
                .ObserveOn(schedulerService.MainScheduler)
                .Subscribe(x => this.Progress = x)
                .AddTo(this.disposables);
        }

        public Exercise Model => this.model;

        public string Name => this.model.Name;

        public TimeSpan Duration => this.model.Duration;

        public TimeSpan ProgressTimeSpan
        {
            get { return this.progressTimeSpan; }
            private set { this.RaiseAndSetIfChanged(ref this.progressTimeSpan, value); }
        }

        public double Progress
        {
            get { return this.progress; }
            private set { this.RaiseAndSetIfChanged(ref this.progress, value); }
        }

        public bool IsActive
        {
            get { return this.isActive; }
            private set { this.RaiseAndSetIfChanged(ref this.isActive, value); }
        }

        private ExecutionContext ExecutionContext
        {
            get { return this.executionContext; }
            set { this.RaiseAndSetIfChanged(ref this.executionContext, value); }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                this.disposables.Dispose();
            }
        }
    }
}