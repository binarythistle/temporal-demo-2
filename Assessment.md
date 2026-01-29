# Staff Developer Advocate Homework Assignment

**Objective:**  
Take a code sample you have previously written and "Temporalize" it by transforming it into a Temporal workflow. You will then present to Temporal engineers as if you were explaining it to a developer audience at a meetup or a conference.

**Instructions:**

1. Select a Code Sample from your personal experience:  
   1. Choose an existing project, function, or service you have written in any of the [Temporal SDK programming languages](https://docs.temporal.io/encyclopedia/temporal-sdks). The complexity should be appropriate for demonstrating workflow orchestration benefits.  
2. Convert to a Temporal Application:  
   1. Identify areas where state management, retries, timeouts, or durability would benefit from Temporal.  
   2. Choose a [Temporal SDK](https://docs.temporal.io/encyclopedia/temporal-sdks#official-sdks) that matches your preferred language (Go, Java, TypeScript, Python, etc.).  
   3. Ensure the workflow is testable and resilient.  
   4. **Minimum requirements to be implemented: the Temporal Worker, Workflow, and Activity constructs.**  
3. Prepare a Presentation (Talking Points):

   Imagine you're explaining this to a team of developers or Temporal customers. Address the following:

* What was the original problem and how did the code work before?  
* How did you break apart what you had before, and how did you go about Temporalizing it? What was your rationale?  
* What challenges existed (e.g., failure handling, state management, scaling issues)?  
* Why does the Temporalized version improve?  
* What trade-offs or considerations did you make?  
* How would you document or teach this to a developer audience?

Deliverables:

* **Code Sample**: Provide both the before and after versions of the code, including a README that explains how to get it up and running.  
  * Share this code example ***at least 48 hours*** **before** the technical assessment, so that the panel members have the opportunity to review in advance  
* **Presentation**: Be prepared to present a short (10-15 min) walkthrough live (note this will be recorded to allow team members that are timezone dispersed to review)

Evaluation Criteria:

* **Technical Depth**: Does the Temporalized version effectively use workflows and activities?  
* **Clarity of Explanatio**n: Is the problem, solution, and benefit well-articulated?  
* **Developer Empath**y: Would this explanation resonate with an external audience?  
* **Code Quality**: Is the implementation clean, idiomatic, and well-documented?

Resources for learning:

* [Getting Started with Temporal](https://learn.temporal.io/getting_started/)  
* [Temporal Documentation](https://docs.temporal.io/)  
* Temporal Samples Repositories  
  * [Java](https://github.com/temporalio/samples-java)  
  * [Go](https://github.com/temporalio/samples-go)  
  * [TypeScript](https://github.com/temporalio/samples-typescript)  
  * [Python](https://github.com/temporalio/samples-python)  
  * .[NET](https://github.com/temporalio/samples-dotnet)  
* Temporal Language SDK Docs  
  * [Java](https://www.javadoc.io/doc/io.temporal/temporal-sdk/latest/index.html)  
  * [Go](https://pkg.go.dev/go.temporal.io/sdk)  
  * [TypeScript](https://typescript.temporal.io/)  
  * [Python](https://python.temporal.io/)  
  * [.NET](https://dotnet.temporal.io/api/)

