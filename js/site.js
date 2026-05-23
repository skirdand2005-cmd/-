// =========================
// CATEGORY SEARCH
// =========================

document.addEventListener("DOMContentLoaded", function () {

    // =========================
    // CATEGORY SEARCH
    // =========================

    const categorySearch =
        document.querySelector(".category-search");

    const categoryLinks =
        document.querySelectorAll(".category-link");

    if (categorySearch) {

        categorySearch.addEventListener("input", function () {

            const value =
                this.value.toLowerCase().trim();

            categoryLinks.forEach(link => {

                const text =
                    link.innerText.toLowerCase();

                if (text.includes(value)) {

                    link.style.display = "block";

                } else {

                    link.style.display = "none";
                }
            });
        });
    }

    // =========================
    // BRAND SEARCH
    // =========================

    const brandSearch =
        document.querySelector(".brand-search");

    const brandItems =
        document.querySelectorAll(".brand-item");

    if (brandSearch) {

        brandSearch.addEventListener("input", function () {

            const value =
                this.value.toLowerCase().trim();

            brandItems.forEach(item => {

                const text =
                    item.innerText.toLowerCase();

                if (text.includes(value)) {

                    item.style.display = "flex";

                } else {

                    item.style.display = "none";
                }
            });
        });
    }

    // =========================
    // BRAND CHECKBOX FILTER
    // =========================

    const brandCheckboxes =
        document.querySelectorAll(".brand-checkbox");

    const productCards =
        document.querySelectorAll(".product-card-wrapper");

    brandCheckboxes.forEach(checkbox => {

        checkbox.addEventListener("change", filterProducts);
    });

    function filterProducts() {

        const selectedBrands = [];

        brandCheckboxes.forEach(cb => {

            if (cb.checked) {

                selectedBrands.push(
                    cb.dataset.brand.toLowerCase()
                );
            }
        });

        productCards.forEach(card => {

            const brand =
                card.dataset.brand.toLowerCase();

            if (
                selectedBrands.length === 0 ||
                selectedBrands.includes(brand)
            ) {

                card.style.display = "block";

            } else {

                card.style.display = "none";
            }
        });
    }

});