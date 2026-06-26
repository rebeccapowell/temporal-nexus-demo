import { useCallback, useEffect, useState } from 'react';
import { api, CheckoutStatusLabel, type CheckoutState, type Product, type PurchaseIntentState } from './api';

export default function App() {
  const [products, setProducts] = useState<Product[]>([]);
  const [pi, setPi] = useState<PurchaseIntentState | null>(null);
  const [checkout, setCheckout] = useState<CheckoutState | null>(null);
  const [email, setEmail] = useState('');
  const [emailSent, setEmailSent] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [shippingForm, setShippingForm] = useState({ fullName: '', line1: '', city: '', postalCode: '', country: 'GB' });
  const [paymentForm, setPaymentForm] = useState({ cardHolder: '', maskedNumber: '4242 4242 4242 4242', expiryMonth: '12', expiryYear: '2027' });

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [prods, intent] = await Promise.all([api.getProducts(), api.getPurchaseIntent()]);
      setProducts(prods);
      setPi(intent);
      if (intent.checkoutId) {
        const currentCheckout = await api.getCheckout(intent.checkoutId);
        setCheckout(currentCheckout);
      } else {
        setCheckout(null);
      }
    } catch (e) {
      setError(String(e));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const handle = (fn: () => Promise<unknown>) => async () => {
    setError(null);
    try {
      await fn();
      await load();
    } catch (e) {
      setError(String(e));
    }
  };

  const total = pi?.items.reduce((sum, item) => sum + item.unitPrice * item.quantity, 0) ?? 0;

  return (
    <div style={{ maxWidth: 1100, margin: '0 auto', padding: 24 }}>
      <header style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <h1 style={{ margin: 0 }}>⚡ Temporal Nexus Demo Shop</h1>
        <div style={{ display: 'flex', gap: 8 }}>
          <button onClick={handle(() => api.newCleanCustomer())}>New Customer</button>
          <a href="http://localhost:8025" target="_blank" rel="noreferrer">📧 MailPit</a>
        </div>
      </header>

      {error && <div style={{ background: '#fee2e2', padding: 12, borderRadius: 6, marginBottom: 16, color: '#991b1b' }}>{error}</div>}
      {loading && <div style={{ color: '#6b7280' }}>Loading…</div>}

      <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr', gap: 24 }}>
        <div>
          <h2>Products</h2>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: 16 }}>
            {products.map((product) => (
              <div key={product.sku} style={{ border: '1px solid #e5e7eb', borderRadius: 8, padding: 16 }}>
                <strong>{product.name}</strong>
                <p style={{ color: '#6b7280', fontSize: 13 }}>{product.description}</p>
                <p style={{ fontWeight: 700 }}>${product.price.toFixed(2)}</p>
                <button
                  onClick={handle(() => api.addItem(product.sku, 1))}
                  style={{ background: '#3b82f6', color: '#fff', border: 'none', padding: '8px 16px', borderRadius: 6 }}
                >
                  Add to Basket
                </button>
              </div>
            ))}
          </div>
        </div>

        <div>
          <h2>Basket {pi?.items.length ? `(${pi.items.length} items)` : ''}</h2>
          {!pi?.items.length ? (
            <p style={{ color: '#6b7280' }}>Empty</p>
          ) : (
            <>
              {pi.items.map((item) => (
                <div key={item.sku} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8, gap: 8 }}>
                  <span>{item.name} ×{item.quantity}</span>
                  <span>${(item.unitPrice * item.quantity).toFixed(2)}</span>
                  <button onClick={handle(() => api.removeItem(item.sku))} style={{ background: 'none', border: 'none', color: '#ef4444' }}>✕</button>
                </div>
              ))}
              <p style={{ fontWeight: 700, borderTop: '1px solid #e5e7eb', paddingTop: 8 }}>Total: ${total.toFixed(2)}</p>
            </>
          )}

          <h3>Email</h3>
          {pi?.email ? (
            <p>{pi.email} {pi.emailVerified ? '✅' : '⏳ (unverified — check MailPit)'}</p>
          ) : (
            <div style={{ display: 'flex', gap: 8 }}>
              <input
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="your@email.com"
                style={{ flex: 1, border: '1px solid #d1d5db', borderRadius: 6, padding: '6px 10px' }}
              />
              <button
                onClick={handle(async () => {
                  await api.provideEmail(email);
                  setEmailSent(true);
                })}
                style={{ background: '#3b82f6', color: '#fff', border: 'none', padding: '6px 14px', borderRadius: 6 }}
              >
                Verify
              </button>
            </div>
          )}
          {emailSent && !pi?.emailVerified && <p style={{ fontSize: 12, color: '#6b7280' }}>📬 Check MailPit for the verification link.</p>}

          {!pi?.checkoutId && (
            <button
              onClick={handle(() => api.startCheckout())}
              style={{ width: '100%', marginTop: 16, background: '#10b981', color: '#fff', border: 'none', padding: '10px', borderRadius: 8, fontWeight: 700 }}
            >
              Start Checkout
            </button>
          )}

          {checkout && (
            <div style={{ marginTop: 16, border: '1px solid #e5e7eb', borderRadius: 8, padding: 16 }}>
              <h3 style={{ margin: '0 0 8px' }}>Checkout: {CheckoutStatusLabel[checkout.status]}</h3>
              {checkout.failureReason && <p style={{ color: '#ef4444' }}>{checkout.failureReason}</p>}

              {!checkout.shippingAddress && checkout.status === 0 && (
                <div>
                  <h4>Shipping Address</h4>
                  <input value={shippingForm.fullName} onChange={(e) => setShippingForm((s) => ({ ...s, fullName: e.target.value }))} placeholder="Full Name" style={{ display: 'block', width: '100%', marginBottom: 6, border: '1px solid #d1d5db', borderRadius: 4, padding: '6px 8px' }} />
                  <input value={shippingForm.line1} onChange={(e) => setShippingForm((s) => ({ ...s, line1: e.target.value }))} placeholder="Address Line 1" style={{ display: 'block', width: '100%', marginBottom: 6, border: '1px solid #d1d5db', borderRadius: 4, padding: '6px 8px' }} />
                  <input value={shippingForm.city} onChange={(e) => setShippingForm((s) => ({ ...s, city: e.target.value }))} placeholder="City" style={{ display: 'block', width: '100%', marginBottom: 6, border: '1px solid #d1d5db', borderRadius: 4, padding: '6px 8px' }} />
                  <input value={shippingForm.postalCode} onChange={(e) => setShippingForm((s) => ({ ...s, postalCode: e.target.value }))} placeholder="Postal Code" style={{ display: 'block', width: '100%', marginBottom: 8, border: '1px solid #d1d5db', borderRadius: 4, padding: '6px 8px' }} />
                  <button onClick={handle(() => api.provideShipping(checkout.checkoutId, { ...shippingForm }))} style={{ background: '#3b82f6', color: '#fff', border: 'none', padding: '8px 16px', borderRadius: 6 }}>Save Address</button>
                </div>
              )}

              {!checkout.payment && checkout.status === 0 && (
                <div style={{ marginTop: 12 }}>
                  <h4>Payment</h4>
                  <input value={paymentForm.cardHolder} onChange={(e) => setPaymentForm((p) => ({ ...p, cardHolder: e.target.value }))} placeholder="Cardholder name" style={{ display: 'block', width: '100%', marginBottom: 6, border: '1px solid #d1d5db', borderRadius: 4, padding: '6px 8px' }} />
                  <button onClick={handle(() => api.providePayment(checkout.checkoutId, { ...paymentForm }))} style={{ background: '#3b82f6', color: '#fff', border: 'none', padding: '8px 16px', borderRadius: 6 }}>Save Payment</button>
                </div>
              )}

              {checkout.status === 5 && (
                <button onClick={handle(() => api.retryPayment(checkout.checkoutId))} style={{ marginTop: 8, background: '#f59e0b', color: '#fff', border: 'none', padding: '8px 16px', borderRadius: 6 }}>Retry Payment</button>
              )}

              {checkout.status < 4 && (
                <button onClick={handle(() => api.cancelCheckout(checkout.checkoutId))} style={{ marginTop: 8, background: '#ef4444', color: '#fff', border: 'none', padding: '8px 16px', borderRadius: 6 }}>Cancel</button>
              )}
            </div>
          )}
        </div>
      </div>

      {pi && (
        <div style={{ marginTop: 24, background: '#f3f4f6', borderRadius: 8, padding: 12, fontSize: 12, color: '#6b7280' }}>
          <strong>Debug:</strong> Purchase Intent: {pi.purchaseIntentId} · Status: {pi.status}
        </div>
      )}
    </div>
  );
}
