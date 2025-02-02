# SpecTemplate Analyzers

This Roslyn analyzer package helps prevent runtime errors when using [`Raffinert.Spec`](https://github.com/Raffinert/Raffinert.Spec).

## Purpose

- Ensures correct usage of `SpecTemplate<TSample>.Adapt<TN>()` by validating that `TN` contains all required members from `TTemplate` at **compile-time** instead of failing at runtime.
- Enforces correct usage of `SpecTemplate.Create(...)`, requiring the first argument to be an **anonymous type projection** (e.g., `p => new { p.Name }`) to prevent invalid expressions.

By catching these issues early, this analyzer enhances code reliability and prevents unexpected failures when adapting specifications.

## Repository

For more details, visit [`Raffinert.Spec`](https://github.com/Raffinert/Raffinert.Spec).
