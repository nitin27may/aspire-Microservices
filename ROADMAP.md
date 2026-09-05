# Roadmap

This project is a reference implementation for .NET Aspire microservices patterns. The roadmap below tracks planned improvements — contributions against any item are welcome (see [CONTRIBUTING.md](CONTRIBUTING.md)).

## Now

- [x] Upgrade to .NET 10 and .NET Aspire 13.x
- [x] Move RabbitMQ consumers/producers to the fully-async RabbitMQ.Client 7.x API
- [x] CI workflow (build + format check) on every PR
- [ ] Integration tests for each API using `Aspire.Hosting.Testing` + WebApplicationFactory
- [ ] Health check UI / readiness dashboard beyond the default Aspire Dashboard

## Next

- [ ] Outbox pattern for the Orders → RabbitMQ event publish (currently best-effort, not transactional with the DB write)
- [ ] API gateway (YARP) in front of the five services instead of the Blazor UI calling each service directly
- [ ] Rate limiting and output caching on the Product Catalog API
- [ ] Structured authorization policies (roles/claims) beyond "authenticated or not"
- [ ] Container image publishing (`dotnet publish /t:PublishContainer`) and a docker-compose fallback for readers without the Aspire CLI

## Later

- [ ] Deployment manifests for Azure Container Apps via `azd` (Aspire's native deployment target)
- [ ] Kubernetes/Helm chart alternative for readers targeting AKS
- [ ] Saga/choreography example for a multi-step order-cancellation flow
- [ ] Wire OpenTelemetry export to a persistent backend (e.g. Grafana/Jaeger) as an alternative to the ephemeral Aspire Dashboard

## Out of Scope

- Payment processing integration (this is a demo, not a PCI-DSS-compliant reference)
- Multi-tenancy — the demo is intentionally single-tenant to keep the data model simple

Have a suggestion? Open an issue using the [feature request template](.github/ISSUE_TEMPLATE/feature_request.md).
