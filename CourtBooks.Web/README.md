# CourtBooks Web

Mobile-first React/TypeScript foundation for CourtBooks.

## Goals

- Responsive web application for desktop, tablet and phone.
- PWA-ready structure for installation on mobile devices.
- Cloudflare Workers Static Assets deployment.
- API integration will be added as the next implementation phase.

## UI rule

The application UI and project work pages use plain text and professional interface controls. Do not use emoji icons.

## Local development

Requires Node.js and npm.

```bash
npm install
npm run dev
```

## Production build

```bash
npm run build
npx wrangler deploy
```
