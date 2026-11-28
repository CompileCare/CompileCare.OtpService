# HeapCare OTP Service

OTP delivery and verification service for the HeapCare healthcare platform.

> **Current status: SECURITY HOLD — NOT IMPLEMENTED AND NOT APPROVED FOR DEPLOYMENT.**

The default branch currently contains only this scaffold README and `.gitignore`. It has no service projects, namespaces, package IDs, runtime configuration, provider adapters, persistence model or deployment resources. This repository must not be treated as an operational OTP service.

## Security hold

A prior proof-of-concept implementation was submitted in closed, unmerged PR #1. It is not an approved implementation and must not be restored or deployed as-is. The review recorded these material gaps:

- OTP plaintext was stored in the cache instead of a keyed verifier/digest.
- Verification used separate read and remove operations, leaving a concurrent replay/race window.
- Attempt limits and robust recipient/IP/tenant rate limits were missing.
- The cooldown check-and-set was not atomic across service instances.
- Recipient validation, purpose binding, endpoint abuse controls and privacy-safe responses needed redesign.
- The delivery/logging paths did not meet the required no-secret/no-recipient-PII telemetry boundary.
- Expiry, resend, invalid-recipient, provider-failure and concurrency tests were incomplete.

**Credential history warning:** PR #1 explicitly records that an SMTP provider credential had previously been committed to repository history. The credential must be treated as compromised until the owning provider/account administrator confirms revocation and replacement. No such owner confirmation is recorded in this repository. Do not reuse it, attempt to recover it from Git history, paste it into a file/issue/PR, or assume that removing it from a configuration file revokes it. Repository history cleanup must be coordinated only after the credential is revoked and downstream use has been checked.

No OTP provider credentials, real recipient values or challenge material belong in source control, tests, logs, traces, metrics, screenshots or PR comments.

## Required design and release gates

Implementation must remain blocked until the service contract, user/purpose binding, delivery channel(s), provider, secret custody and hospital/privacy responsibilities are explicitly decided. The design must align with HeapCare's [ADR-012 contact-data and verification boundaries](https://github.com/HeapCare/HeapCare.Doc/blob/main/architecture/decisions/ADR-012-verified-patient-contact-data-and-matching.md).

The next implementation proposal must cover, at minimum:

1. **Purpose and subject binding.** Bind each challenge to a tenant, a verified server-side subject/contact reference, a single approved purpose and a one-time challenge identifier. Do not infer ownership or identity from an email address or phone number. A challenge for one patient/contact/purpose must not satisfy another.
2. **Cryptographic generation and storage.** Use a cryptographically secure random generator with an approved entropy/length policy. Persist only a keyed challenge verifier (for example HMAC using a deployment-managed key), with key version and constant-time comparison. Never store or log OTP plaintext or use an unkeyed hash of a low-entropy code.
3. **Atomic one-time verification.** Enforce expiry, maximum attempts, resend invalidation and single-use consumption atomically across concurrent requests and across service replicas. A successful verification may be consumed only once. Do not implement verification as a separate cache read followed by delete.
4. **Distributed abuse controls.** Use atomic shared quotas/cooldowns across replicas, with independently reviewed limits for destination, subject, tenant, network/source and delivery provider. Define safe lockout/cooldown behavior that does not enable trivial account denial-of-service or reveal whether an account/contact exists.
5. **Enumeration-resistant API.** Validate recipient/channel formats and request shape. Use generic public responses for unknown, expired, used, invalid or throttled challenges where required by the threat model. Keep detailed internal reasons in protected operational telemetry without recipient PII or codes.
6. **Reliable delivery.** Select an approved SMS/email provider and sender identity. Specify idempotency, delivery outbox behavior, retry/backoff, provider timeout/failure, duplicate suppression and dead-letter handling. Never report verification success because a message was merely accepted by a provider.
7. **Secrets and operations.** Store provider credentials, HMAC keys and encryption keys in the approved secret/KMS system, with least privilege, access audit, expiry alerts, rotation and emergency revocation. No provider value or signing key is committed to Git.
8. **Audit and privacy.** Audit initiation, delivery outcome, verification result, expiry, lockout and privileged actions using opaque IDs, purpose, tenant, actor, timestamp and correlation ID. Exclude OTPs, challenge secrets, full contact values and raw recipient identifiers from audit payloads and telemetry. Confirm lawful purposes, notice/consent wording, retention, deletion, residency and incident ownership with qualified privacy/security owners.
9. **Automated verification.** Test expiry boundaries, wrong-code attempt cap, replay, two concurrent correct verifications (at most one succeeds), resend invalidating the previous code, cross-tenant and cross-purpose rejection, recipient normalization, rate limiting across multiple service replicas, enumeration-resistant responses, provider failures/retries and absence of secrets/PII in all captured logs.
10. **Release proof.** Pass unit, integration, concurrency, security and Docker tests against the selected shared store/provider adapter. Complete secret-history remediation, provider-owner rotation confirmation, threat-model review, penetration test findings, monitoring/runbooks, backup/restore and an approved rollback procedure before any pilot deployment.

Do not hardcode TTL, attempt, resend, cooldown or retention values until the product, security and hospital owners approve the policy for the intended channel and deployment. A development cache or in-memory limiter alone is not sufficient for distributed enforcement.

## Safe re-entry sequence

1. Obtain written confirmation from the SMTP credential owner that the historical credential is revoked and replaced; verify that dependent systems no longer accept it.
2. Complete a coordinated secret-history review/remediation after revocation. Do not rewrite shared Git history without organization-owner approval and a developer/consumer coordination plan.
3. Approve the threat model, API contract, data model, provider, purposes, channel policy, key management, limits, retention and incident owner.
4. Implement the provider-neutral challenge core and atomic persistence/rate-limit model with synthetic test data only.
5. Add the selected delivery adapter using managed secrets; verify delivery failures, retries and abuse scenarios without exposing challenge material.
6. Integrate with patient contact verification only after ADR-012 implementation gates and role/purpose mapping are approved.
7. Run full security and operational acceptance; keep deployment disabled until explicit pilot/release sign-off.

## Scope boundary

This service is for short-lived proof-of-control challenges only. It must not become an authentication/authorization authority, determine patient identity, act as consent to messaging, or independently change patient records, MRNs, memberships or clinical data. An OTP proves control of a channel at a point in time; it does not prove that the channel is uniquely owned by the patient.

This README records a security hold and re-entry criteria. It is not evidence that the credential has been revoked, that the service is implemented, or that any delivery provider is configured.
