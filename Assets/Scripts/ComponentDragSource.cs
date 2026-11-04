
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ComponentDragSource : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [SerializeField]
    private AssemblyController assemblyController;

    private Image backgroundImage;
    private Color originalColor;

    private void Awake()
    {
        backgroundImage = GetComponent<Image>();
        originalColor = backgroundImage.color;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (assemblyController == null)
            return;

        assemblyController.BeginComponentDrag(
            eventData.position
        );

        // Скрываем фон карточки,
        // чтобы он не перекрывал 3D-деталь.
        Color transparentColor = originalColor;
        transparentColor.a = 0f;

        backgroundImage.color = transparentColor;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (assemblyController != null)
        {
            assemblyController.UpdateComponentDrag(
                eventData.position
            );
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (assemblyController != null)
        {
            assemblyController.EndComponentDrag(
                eventData.position
            );
        }

        // Возвращаем исходный цвет карточки.
        backgroundImage.color = originalColor;
    }

    private void OnDisable()
    {
        if (backgroundImage != null)
            backgroundImage.color = originalColor;
    }
}
