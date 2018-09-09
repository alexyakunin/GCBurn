package common

import "runtime"

type IActivity interface {
	Run()
}

type ParallelRunner struct {
	Activities []IActivity
}

var ThreadCount = runtime.NumCPU()

func NewParallelRunner(factory func(index int) IActivity) *ParallelRunner {
	return NewParallelRunnerEx(factory, ThreadCount)
}

func NewParallelRunnerEx(factory func(index int) IActivity, threadCount int) *ParallelRunner {
	r := &ParallelRunner{}
	activities := make([]IActivity, 0)
	for i := 0; i < threadCount; i++ {
		activities = append(activities, factory(i))
	}
	r.Activities = activities
	return r
}

func (r ParallelRunner) Run() []IActivity {
	var done = make(chan bool)
	// Doing this step separately to make sure we run all the activities at almost the same time
	for _, a := range r.Activities {
		activity := a // Necessary: a is a part of closure
		go func() {
			activity.Run()
			done <- true
		}()
	}
	// Waiting for goroutines to complete
	for range r.Activities {
		<-done
	}
	return r.Activities
}
